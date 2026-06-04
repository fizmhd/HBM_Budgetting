using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Infrastructure.Options;
using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Api.Services.Mappers;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.Results;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;


using SessionOptions = BudgetTracker.Api.Infrastructure.Options.SessionOptions;

namespace BudgetTracker.Api.Services;

/// <summary>
/// Application-level authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly IAuthProvider _authProvider;
    private readonly IUserResolutionService _userResolutionService;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserMapper _userMapper;
    private readonly LockoutOptions _lockoutOptions;
    private readonly SessionOptions _sessionOptions;
    private readonly AuthOptions _authOptions;
    private readonly ILogger<AuthService> _logger;




    public AuthService(
        IAuthProvider authProvider,
        IUserResolutionService userResolutionService,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        UserMapper userMapper,
        IOptions<LockoutOptions> lockoutOptions,
        IOptions<SessionOptions> sessionOptions,
        IOptions<AuthOptions> authOptions,
        ILogger<AuthService> logger)

    {
        _authProvider = authProvider;
        _userResolutionService = userResolutionService;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _userMapper = userMapper;
        _lockoutOptions = lockoutOptions.Value;
        _sessionOptions = sessionOptions.Value;
        _authOptions = authOptions.Value;
        _logger = logger;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Call external auth provider to register
            var providerResult = await _authProvider.RegisterAsync(request.Email, request.Password);
            if (providerResult.IsFailure)
            {
                return Result<AuthResponse>.Failure(providerResult.Errors);
            }

            var providerResponse = providerResult.Value;

            // Resolve/create internal user
            var userResult = await _userResolutionService.ResolveUserAsync(
                AuthenticationConstants.SupabaseProviderName,
                providerResponse.ExternalUserId,
                providerResponse.Email);

            if (userResult.IsFailure)
            {
                return Result<AuthResponse>.Failure(userResult.Errors);
            }

            var user = userResult.Value;

            // Create refresh token record
            await CreateRefreshTokenAsync(user.Id, providerResponse.RefreshToken, cancellationToken);

            // Map to response
            var authResponse = new AuthResponse
            {
                AccessToken = providerResponse.AccessToken,
                RefreshToken = providerResponse.RefreshToken,
                ExpiresAt = providerResponse.ExpiresAt,
                User = _userMapper.ToDto(user)
            };

            _logger.LogInformation("User {UserId} registered successfully", user.Id);
            return Result<AuthResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for email {Email}", request.Email);
            return Error.Internal("REGISTRATION_ERROR", "An unexpected error occurred during registration");
        }
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if user exists and is locked out
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser != null)
            {
                var lockoutCheck = CheckLockoutStatus(existingUser);
                if (lockoutCheck.IsFailure)
                {
                    return Result<AuthResponse>.Failure(lockoutCheck.Errors);
                }
            }

            // Call external auth provider to login
            var providerResult = await _authProvider.LoginAsync(request.Email, request.Password);
            if (providerResult.IsFailure)
            {
                // Increment failed attempts if user exists
                if (existingUser != null)
                {
                    await HandleFailedLoginAsync(existingUser, cancellationToken);
                }

                return Result<AuthResponse>.Failure(providerResult.Errors);
            }

            var providerResponse = providerResult.Value;

            // Resolve internal user
            var userResult = await _userResolutionService.ResolveUserAsync(
                AuthenticationConstants.SupabaseProviderName,
                providerResponse.ExternalUserId,
                providerResponse.Email);

            if (userResult.IsFailure)
            {
                return Result<AuthResponse>.Failure(userResult.Errors);
            }

            var user = userResult.Value;

            // Reset failed login attempts on successful login
            if (user.FailedLoginAttempts > 0 || user.LockoutEndUtc.HasValue)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEndUtc = null;
                user.LastFailedLoginAttemptUtc = null;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Create refresh token record
            await CreateRefreshTokenAsync(user.Id, providerResponse.RefreshToken, cancellationToken);

            // Check session limits and evict oldest if needed
            await EnforceSessionLimitsAsync(user.Id, cancellationToken);

            // Map to response
            var authResponse = new AuthResponse
            {
                AccessToken = providerResponse.AccessToken,
                ExpiresAt = providerResponse.ExpiresAt,
                User = _userMapper.ToDto(user)
            };

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            _logger.LogInformation("Returning AuthResponse - Token Length: {TokenLength}, ExpiresAt: {ExpiresAt}", 
                authResponse.AccessToken?.Length ?? 0, 
                authResponse.ExpiresAt);

            // Return response with refresh token (to be handled by endpoint)
            authResponse.RefreshToken = providerResponse.RefreshToken;
            return Result<AuthResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for email {Email}", request.Email);
            return Error.Internal("LOGIN_ERROR", "An unexpected error occurred during login");
        }
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Revoke the refresh token in the database
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var tokenHash = HashToken(refreshToken);
                var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

                if (storedToken != null && storedToken.IsActive)
                {
                    storedToken.RevokedAt = DateTime.UtcNow;
                    _refreshTokenRepository.Update(storedToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Refresh token revoked during logout for user {UserId}", storedToken.UserId);
                }
                else
                {
                    _logger.LogWarning("Refresh token not found or already revoked during logout");
                }
            }

            // Sign out from the auth provider (Supabase)
            var result = await _authProvider.LogoutAsync(string.Empty);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout");
            return Result.Failure(Error.Internal("LOGOUT_ERROR", "An unexpected error occurred during logout"));
        }
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Hash the incoming refresh token
            var tokenHash = HashToken(refreshToken);

            // Find the refresh token record
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            if (storedToken == null)
            {
                _logger.LogWarning("Refresh token not found");
                return Error.Unauthorized("INVALID_TOKEN", "Invalid or expired refresh token");
            }

            // Check if token has been replaced (reuse detection)
            if (storedToken.ReplacedByTokenId.HasValue)
            {
                var gracePeriodEnd = storedToken.RevokedAt?.AddSeconds(_authOptions.RefreshTokenGracePeriodSeconds);
                
                if (gracePeriodEnd == null || DateTime.UtcNow > gracePeriodEnd)
                {
                    // Token reuse detected outside grace period - revoke entire family
                    _logger.LogWarning(
                        "Token reuse detected for user {UserId}, family {FamilyId}. Revoking entire family.",
                        storedToken.UserId, storedToken.FamilyId);
                    
                    await _refreshTokenRepository.RevokeByFamilyIdAsync(storedToken.FamilyId, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
                    return Error.Unauthorized("TOKEN_REUSE_DETECTED", "Token reuse detected. All sessions have been revoked. Please login again.");
                }
                
                _logger.LogDebug("Token reuse within grace period for user {UserId}", storedToken.UserId);
            }

            // Check if token is expired or revoked
            if (!storedToken.IsActive)
            {
                _logger.LogWarning("Refresh token is inactive for user {UserId}", storedToken.UserId);
                return Error.Unauthorized("INVALID_TOKEN", "Invalid or expired refresh token");
            }

            // Get the user
            var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
            if (user == null || !user.IsActive)
            {
                _logger.LogWarning("User {UserId} not found or inactive", storedToken.UserId);
                return Error.Unauthorized("USER_INACTIVE", "User account is inactive");
            }

            // Call external auth provider to refresh
            var providerResult = await _authProvider.RefreshTokenAsync(refreshToken);
            if (providerResult.IsFailure)
            {
                // Revoke the token family on failure (potential token theft)
                await _refreshTokenRepository.RevokeByFamilyIdAsync(storedToken.FamilyId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result<AuthResponse>.Failure(providerResult.Errors);
            }

            var providerResponse = providerResult.Value;

            // Revoke old token and create new one (token rotation)
            storedToken.RevokedAt = DateTime.UtcNow;
            _refreshTokenRepository.Update(storedToken);

            var newToken = await CreateRefreshTokenAsync(
                user.Id,
                providerResponse.RefreshToken,
                cancellationToken,
                storedToken.FamilyId);

            storedToken.ReplacedByTokenId = newToken.Id;
            _refreshTokenRepository.Update(storedToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to response
            var authResponse = new AuthResponse
            {
                AccessToken = providerResponse.AccessToken,
                ExpiresAt = providerResponse.ExpiresAt,
                User = _userMapper.ToDto(user)
            };

            _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);
            
            // Return response with refresh token (to be handled by endpoint)
            authResponse.RefreshToken = providerResponse.RefreshToken;
            return Result<AuthResponse>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return Error.Internal("REFRESH_ERROR", "An unexpected error occurred during token refresh");
        }
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authProvider.ForgotPasswordAsync(request.Email);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during forgot password for email {Email}", request.Email);
            return Result.Failure(Error.Internal("FORGOT_PASSWORD_ERROR", "An unexpected error occurred"));
        }
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authProvider.ResetPasswordAsync(request.Token, request.NewPassword);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during password reset");
            return Result.Failure(Error.Internal("RESET_PASSWORD_ERROR", "An unexpected error occurred during password reset"));
        }
    }

    public async Task<Result> RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Revoke all refresh tokens
            await _refreshTokenRepository.RevokeAllByUserIdAsync(userId, cancellationToken);

            // Increment token version to invalidate all access tokens
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user != null)
            {
                user.TokenVersion++;
                _userRepository.Update(user);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("All sessions revoked for user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while revoking sessions for user {UserId}", userId);
            return Result.Failure(Error.Internal("REVOKE_SESSIONS_ERROR", "An unexpected error occurred while revoking sessions"));
        }
    }

    public async Task<Result> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authProvider.ConfirmEmailAsync(token);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during email confirmation");
            return Result.Failure(Error.Internal("CONFIRM_EMAIL_ERROR", "An unexpected error occurred during email confirmation"));
        }
    }

    #region Private Helper Methods

    private Result CheckLockoutStatus(User user)
    {
        if (!_lockoutOptions.Enabled)
        {
            return Result.Success();
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            var remainingMinutes = (int)(user.LockoutEndUtc.Value - DateTime.UtcNow).TotalMinutes;
            _logger.LogWarning("User {UserId} is locked out until {LockoutEnd}", user.Id, user.LockoutEndUtc.Value);
            return Result.Failure(Error.Unauthorized("ACCOUNT_LOCKED", $"Account is locked. Try again in {remainingMinutes} minutes."));
        }

        return Result.Success();
    }

    private async Task HandleFailedLoginAsync(User user, CancellationToken cancellationToken)
    {
        if (!_lockoutOptions.Enabled)
        {
            return;
        }

        user.FailedLoginAttempts++;
        user.LastFailedLoginAttemptUtc = DateTime.UtcNow;

        if (user.FailedLoginAttempts >= _lockoutOptions.MaxFailedAccessAttempts)
        {
            user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(_lockoutOptions.LockoutDurationMinutes);
            _logger.LogWarning("User {UserId} locked out until {LockoutEnd} after {Attempts} failed attempts",
                user.Id, user.LockoutEndUtc.Value, user.FailedLoginAttempts);
        }

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken,
        Guid? familyId = null)
    {
        var tokenHash = HashToken(refreshToken);

        var newToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            FamilyId = familyId ?? Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(_sessionOptions.TimeoutMinutes),
            RevokedAt = null
        };

        await _refreshTokenRepository.AddAsync(newToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return newToken;
    }

    private async Task EnforceSessionLimitsAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Get all active tokens for the user
        var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
        
        var tokenCount = activeTokens.Count();
        if (tokenCount > _sessionOptions.MaxConcurrentSessions)
        {
            // Revoke oldest tokens to enforce limit
            var tokensToRevoke = activeTokens
                .OrderBy(t => t.CreatedAt)
                .Take(tokenCount - _sessionOptions.MaxConcurrentSessions)
                .ToList();
            
            foreach (var token in tokensToRevoke)
            {
                token.RevokedAt = DateTime.UtcNow;
                _refreshTokenRepository.Update(token);
            }
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation(
                "Revoked {Count} oldest sessions for user {UserId} to enforce session limit of {Limit}",
                tokensToRevoke.Count, userId, _sessionOptions.MaxConcurrentSessions);
        }
    }

    private string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    #endregion
}
