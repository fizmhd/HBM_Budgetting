using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Interface for external authentication providers (e.g., Supabase, Auth0)
/// </summary>
public interface IAuthProvider
{
    /// <summary>
    /// Registers a new user with email and password
    /// </summary>
    Task<Result<AuthProviderResponse>> RegisterAsync(string email, string password);

    /// <summary>
    /// Authenticates a user with email and password
    /// </summary>
    Task<Result<AuthProviderResponse>> LoginAsync(string email, string password);

    /// <summary>
    /// Logs out a user by invalidating their access token
    /// </summary>
    Task<Result> LogoutAsync(string accessToken);

    /// <summary>
    /// Refreshes an access token using a refresh token
    /// </summary>
    Task<Result<AuthProviderResponse>> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Initiates password reset flow by sending email
    /// </summary>
    Task<Result> ForgotPasswordAsync(string email);

    /// <summary>
    /// Resets password using a token
    /// </summary>
    Task<Result> ResetPasswordAsync(string token, string newPassword);

    /// <summary>
    /// Confirms email using a token
    /// </summary>
    Task<Result> ConfirmEmailAsync(string token);
}
