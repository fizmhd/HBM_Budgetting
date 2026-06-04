namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Entity representing refresh tokens for JWT authentication
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// Foreign key to the User entity
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Hashed value of the refresh token
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Family identifier for token rotation tracking
    /// </summary>
    public Guid FamilyId { get; set; }

    /// <summary>
    /// UTC timestamp when the token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// UTC timestamp when the token was revoked (null if still valid)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Foreign key to the refresh token that replaced this one (null if not replaced)
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Information about the device/client that requested this token (optional)
    /// </summary>
    public string? DeviceInfo { get; set; }

    // Navigation properties
    /// <summary>
    /// The user associated with this refresh token
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// The refresh token that replaced this one (if applicable)
    /// </summary>
    public RefreshToken? ReplacedByToken { get; set; }

    /// <summary>
    /// Helper property to check if token is active
    /// </summary>
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}
