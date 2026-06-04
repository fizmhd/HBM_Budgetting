namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Response from external authentication provider
/// </summary>
public class AuthProviderResponse
{
    /// <summary>
    /// External user ID from the auth provider
    /// </summary>
    public string ExternalUserId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Access token (JWT)
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the access token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Indicates whether the user's email has been confirmed
    /// </summary>
    public bool EmailConfirmed { get; set; }
}
