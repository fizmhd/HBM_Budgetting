namespace BudgetTracker.Api.Infrastructure.Options;

/// <summary>
/// Configuration options for CSRF protection.
/// </summary>
public class CsrfOptions
{
    public const string SectionName = "Csrf";

    /// <summary>
    /// Gets or sets whether CSRF protection is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the CSRF cookie name.
    /// </summary>
    public string CookieName { get; set; } = ".BudgetTracker.Csrf";

    /// <summary>
    /// Gets or sets the CSRF header name.
    /// </summary>
    public string HeaderName { get; set; } = "X-CSRF-TOKEN";

    /// <summary>
    /// Gets or sets whether the CSRF cookie is HTTP only.
    /// </summary>
    public bool CookieHttpOnly { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the CSRF cookie is secure (HTTPS only).
    /// </summary>
    public bool CookieSecure { get; set; } = true;

    /// <summary>
    /// Gets or sets the SameSite mode for the CSRF cookie.
    /// </summary>
    public string CookieSameSite { get; set; } = "Strict";

    /// <summary>
    /// Gets or sets the token expiration time in minutes.
    /// </summary>
    public int TokenExpirationMinutes { get; set; } = 60;
}
