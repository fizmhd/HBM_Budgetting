namespace BudgetTracker.Api.Infrastructure.Options;

/// <summary>
/// Configuration options for account lockout.
/// </summary>
public class LockoutOptions
{
    public const string SectionName = "Security:Lockout";

    /// <summary>
    /// Gets or sets whether lockout is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of failed login attempts before lockout.
    /// </summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the lockout duration in minutes.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets whether to allow lockout for new users.
    /// </summary>
    public bool AllowedForNewUsers { get; set; } = true;

    /// <summary>
    /// Gets or sets the time window in minutes for counting failed attempts.
    /// </summary>
    public int FailedAttemptsWindowMinutes { get; set; } = 10;
}
