using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Api.Services.Mappers;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.DTOs.Users;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Services;

/// <summary>
/// Implementation of user management service
/// </summary>
public class UserService : IUserService
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserMapper _userMapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        UserMapper userMapper,
        ILogger<UserService> logger)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _userMapper = userMapper;
        _logger = logger;
    }

    public async Task<Result<UserDto>> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        var user = await _currentUserService.GetUserAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("GetProfileAsync failed: User not found in current context");
            return Result<UserDto>.Failure(Error.Unauthorized("User.Unauthorized", "User is not authorized"));
        }

        return Result<UserDto>.Success(_userMapper.ToDto(user));
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _currentUserService.GetUserAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("UpdateProfileAsync failed: User not found in current context");
            return Result<UserDto>.Failure(Error.Unauthorized("User.Unauthorized", "User is not authorized"));
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.FirstName))
        {
            user.FirstName = request.FirstName;
        }

        if (!string.IsNullOrEmpty(request.LastName))
        {
            user.LastName = request.LastName;
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated profile", user.Id);
        return Result<UserDto>.Success(_userMapper.ToDto(user));
    }

    public async Task<Result<UserDto>> UpdateSettingsAsync(UpdateSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _currentUserService.GetUserAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("UpdateSettingsAsync failed: User not found in current context");
            return Result<UserDto>.Failure(Error.Unauthorized("User.Unauthorized", "User is not authorized"));
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.PreferredCurrency))
        {
            user.PreferredCurrency = request.PreferredCurrency;
        }

        if (!string.IsNullOrEmpty(request.DateFormat))
        {
            user.DateFormat = request.DateFormat;
        }

        if (!string.IsNullOrEmpty(request.Theme))
        {
            user.Theme = request.Theme;
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} updated settings", user.Id);
        return Result<UserDto>.Success(_userMapper.ToDto(user));
    }

    public async Task<Result<UserDto>> CompleteProfileAsync(CompleteProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _currentUserService.GetUserAsync(cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("CompleteProfileAsync failed: User not found in current context");
            return Result<UserDto>.Failure(Error.Unauthorized("User.Unauthorized", "User is not authorized"));
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IsProfileComplete = true;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} completed profile", user.Id);
        return Result<UserDto>.Success(_userMapper.ToDto(user));
    }
}
