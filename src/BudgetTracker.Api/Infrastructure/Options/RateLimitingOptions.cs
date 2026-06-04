namespace BudgetTracker.Api.Infrastructure.Options;

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Gets or sets whether rate limiting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of requests per time window.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time window in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the queue limit for requests.
    /// </summary>
    public int QueueLimit { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of requests per endpoint.
    /// </summary>
    public int PerEndpointLimit { get; set; } = 50;

    /// <summary>
    /// Gets or sets whether to enable IP-based rate limiting.
    /// </summary>
    public bool EnableIpRateLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable user-based rate limiting.
    /// </summary>
    public bool EnableUserRateLimiting { get; set; } = true;
}
