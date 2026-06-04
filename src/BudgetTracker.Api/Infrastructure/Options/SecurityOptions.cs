namespace BudgetTracker.Api.Infrastructure.Options;

/// <summary>
/// Configuration options for security settings.
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Gets or sets whether to enable HTTPS redirection.
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable HSTS (HTTP Strict Transport Security).
    /// </summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// Gets or sets the HSTS max age in seconds.
    /// </summary>
    public int HstsMaxAgeSeconds { get; set; } = 31536000; // 1 year

    /// <summary>
    /// Gets or sets whether to include subdomains in HSTS.
    /// </summary>
    public bool HstsIncludeSubDomains { get; set; } = true;

    /// <summary>
    /// Gets or sets the allowed CORS origins.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether to allow credentials in CORS.
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum request body size in bytes.
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 10485760; // 10 MB
}
