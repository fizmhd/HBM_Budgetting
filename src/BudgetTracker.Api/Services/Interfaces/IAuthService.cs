using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// Application-level authentication service interface
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user
    /// </summary>
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user and creates a session
    /// </summary>
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the current user and revokes the provided refresh token
    /// </summary>
    Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates password reset flow
    /// </summary>
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets user password
    /// </summary>
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all sessions for a specific user
    /// </summary>
    Task<Result> RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms user email using a token
    /// </summary>
    Task<Result> ConfirmEmailAsync(string token, CancellationToken cancellationToken = default);
}
