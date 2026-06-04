namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Entity representing external authentication provider login information
/// </summary>
public class UserExternalLogin : BaseEntity
{
    /// <summary>
    /// Foreign key to the User entity
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Name of the external authentication provider (e.g., "Google", "GitHub")
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Unique identifier from the external provider
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// Email address from the external provider (optional)
    /// </summary>
    public string? ProviderEmail { get; set; }

    /// <summary>
    /// Timestamp of the last successful login using this provider
    /// </summary>
    public DateTime LastLoginAt { get; set; }

    /// <summary>
    /// Indicates whether this external login is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation property
    /// <summary>
    /// The user associated with this external login
    /// </summary>
    public User User { get; set; } = null!;
}
