namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Membership of a user in a household.
/// </summary>
public class HouseholdMember : BaseEntity
{
    /// <summary>
    /// Household this membership belongs to.
    /// </summary>
    public Guid HouseholdId { get; set; }

    /// <summary>
    /// Internal <see cref="User.Id"/> of the member.
    /// Nullable for the future parent-managed child case; the MVP always sets it.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Name shown for this member within the household.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Role of the member within the household.
    /// </summary>
    public HouseholdRole Role { get; set; } = HouseholdRole.Member;

    /// <summary>
    /// When the member joined the household (UTC).
    /// </summary>
    public DateTime JoinedAt { get; set; }

    // Navigation properties
    /// <summary>
    /// The household this membership belongs to.
    /// </summary>
    public Household? Household { get; set; }
}
