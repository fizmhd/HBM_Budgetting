using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Options;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Api.Services.Mappers;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using BudgetTracker.Api.UnitTests.Helpers;
using FluentAssertions;
using AutoFixture;
using BudgetTracker.Api.Infrastructure.Persistence;

namespace BudgetTracker.Api.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Fixture _fixture = new();
    private readonly IAuthProvider _authProvider = Substitute.For<IAuthProvider>();
    private readonly IUserResolutionService _userResolutionService = Substitute.For<IUserResolutionService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UserMapper _userMapper = new();
    private readonly IOptions<LockoutOptions> _lockoutOptions = Substitute.For<IOptions<LockoutOptions>>();
    private readonly IOptions<SessionOptions> _sessionOptions = Substitute.For<IOptions<SessionOptions>>();
    private readonly IOptions<AuthOptions> _authOptions = Substitute.For<IOptions<AuthOptions>>();
    private readonly ILogger<AuthService> _logger = Substitute.For<ILogger<AuthService>>();
    
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _lockoutOptions.Value.Returns(new LockoutOptions { Enabled = true, MaxFailedAccessAttempts = 5, LockoutDurationMinutes = 15 });
        _sessionOptions.Value.Returns(new SessionOptions { TimeoutMinutes = 60, MaxConcurrentSessions = 5 });
        _authOptions.Value.Returns(new AuthOptions { RefreshTokenGracePeriodSeconds = 60 });

        _sut = new AuthService(
            _authProvider,
            _userResolutionService,
            _userRepository,
            _refreshTokenRepository,
            _unitOfWork,
            _userMapper,
            _lockoutOptions,
            _sessionOptions,
            _authOptions,
            _logger);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var request = _fixture.Create<LoginRequest>();
        var user = TestDataBuilder.CreateUser();
        var providerResponse = _fixture.Create<AuthProviderResponse>();
        
        _userRepository.GetByEmailAsync(request.Email).Returns(user);
        _authProvider.LoginAsync(request.Email, request.Password).Returns(Result<AuthProviderResponse>.Success(providerResponse));
        _userResolutionService.ResolveUserAsync(Arg.Any<string>(), providerResponse.ExternalUserId, providerResponse.Email)
            .Returns(Result<User>.Success(user));
        _refreshTokenRepository.GetActiveByUserIdAsync(user.Id).Returns(new List<RefreshToken>());

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().BeSuccess();
        result.Value.User.Email.Should().Be(user.Email);
        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var request = _fixture.Create<LoginRequest>();
        var error = Error.Unauthorized("INVALID_CREDENTIALS", "Invalid credentials");
        
        _userRepository.GetByEmailAsync(request.Email).Returns((User?)null);
        _authProvider.LoginAsync(request.Email, request.Password).Returns(Result<AuthProviderResponse>.Failure(error));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().BeFailure()
            .And.HaveError(error.Code);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountLocked_ReturnsUnauthorized()
    {
        // Arrange
        var request = _fixture.Create<LoginRequest>();
        var user = TestDataBuilder.CreateUser();
        user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(10);
        
        _userRepository.GetByEmailAsync(request.Email).Returns(user);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().BeFailure()
            .And.HaveError("ACCOUNT_LOCKED");
            
        await _authProvider.DidNotReceive().LoginAsync(Arg.Any<string>(), Arg.Any<string>());
    }
}
