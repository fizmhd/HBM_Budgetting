namespace BudgetTracker.Api.Infrastructure.Options;

/// <summary>
/// Configuration options for authentication.
/// </summary>
public class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Gets or sets the JWT secret key.
    /// </summary>
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT issuer.
    /// </summary>
    public string JwtIssuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT audience.
    /// </summary>
    public string JwtAudience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token expiration time in minutes.
    /// </summary>
    public int TokenExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the refresh token expiration time in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the grace period in seconds for refresh token rotation.
    /// Allows reuse of a replaced token within this window to handle race conditions.
    /// </summary>
    public int RefreshTokenGracePeriodSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to require email confirmation.
    /// </summary>
    public bool RequireEmailConfirmation { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable two-factor authentication.
    /// </summary>
    public bool EnableTwoFactorAuth { get; set; } = false;

    /// <summary>
    /// Gets or sets whether auth cookies require SSL/HTTPS.
    /// </summary>
    public bool CookieSecure { get; set; } = true;

    /// <summary>
    /// Gets or sets the SameSite mode for auth cookies (Strict, Lax, None).
    /// </summary>
    public string CookieSameSite { get; set; } = "Strict";
}
