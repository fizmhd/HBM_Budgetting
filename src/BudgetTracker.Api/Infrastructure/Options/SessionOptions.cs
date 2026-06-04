namespace BudgetTracker.Api.Infrastructure.Options;

/// <summary>
/// Configuration options for session management.
/// </summary>
public class SessionOptions
{
    public const string SectionName = "Session";

    /// <summary>
    /// Gets or sets the session timeout in minutes.
    /// </summary>
    public int TimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets the idle timeout in minutes.
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 20;

    /// <summary>
    /// Gets or sets whether to use sliding expiration.
    /// </summary>
    public bool SlidingExpiration { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of concurrent sessions per user.
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 5;

    /// <summary>
    /// Gets or sets the eviction policy when max concurrent sessions is reached.
    /// </summary>
    public string EvictionPolicy { get; set; } = "OldestFirst";

    /// <summary>
    /// Gets or sets the cookie name for the session.
    /// </summary>
    public string CookieName { get; set; } = ".BudgetTracker.Session";

    /// <summary>
    /// Gets or sets whether the session cookie is HTTP only.
    /// </summary>
    public bool CookieHttpOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the session cookie is secure (HTTPS only).
    /// </summary>
    public bool CookieSecure { get; set; } = true;

    /// <summary>
    /// Gets or sets the SameSite mode for the session cookie.
    /// </summary>
    public string CookieSameSite { get; set; } = "Strict";
}
