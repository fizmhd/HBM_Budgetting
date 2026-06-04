namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// User entity representing application users
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// User's email address (unique)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name (optional)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (optional)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User's preferred display currency code (e.g. SEK, USD, EUR).
    /// This is a per-user display preference, distinct from the app base currency (SEK)
    /// and from a transaction's original currency + FX (introduced in Phase 2).
    /// </summary>
    public string PreferredCurrency { get; set; } = "SEK";

    /// <summary>
    /// User's preferred date format (e.g. yyyy-MM-dd)
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// User's preferred theme (light/dark)
    /// </summary>
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Indicates whether the user has completed their profile
    /// </summary>
    public bool IsProfileComplete { get; set; } = false;

    /// <summary>
    /// Token version for invalidating all refresh tokens
    /// </summary>
    public int TokenVersion { get; set; } = 1;

    /// <summary>
    /// Number of consecutive failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>
    /// UTC timestamp when the account lockout ends (null if not locked)
    /// </summary>
    public DateTime? LockoutEndUtc { get; set; }

    /// <summary>
    /// UTC timestamp of the last failed login attempt
    /// </summary>
    public DateTime? LastFailedLoginAttemptUtc { get; set; }

    /// <summary>
    /// Indicates whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    /// <summary>
    /// Collection of external login providers linked to this user
    /// </summary>
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();

    /// <summary>
    /// Collection of refresh tokens issued to this user
    /// </summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
