namespace BudgetTracker.Api.Infrastructure.Options;

using BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Configuration options for Supabase.
/// </summary>
public class SupabaseOptions
{
    public const string SectionName = AuthenticationConstants.SupabaseSectionName;

    /// <summary>
    /// Gets or sets the Supabase URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Supabase API key (Anon Key).
    /// </summary>
    public string AnonKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Supabase JWT secret.
    /// </summary>
    public string JwtSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service role key (for admin operations).
    /// </summary>
    public string ServiceRoleKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to enable auto-refresh for tokens.
    /// </summary>
    public bool AutoRefreshToken { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to persist the session.
    /// </summary>
    public bool PersistSession { get; set; } = true;
}
