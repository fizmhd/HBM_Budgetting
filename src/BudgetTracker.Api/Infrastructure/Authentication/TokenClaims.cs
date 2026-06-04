namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Claims extracted from a JWT token
/// </summary>
public class TokenClaims
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
    /// UTC timestamp when the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
