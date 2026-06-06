namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// A household groups adult members (owner + invited spouse in the MVP) so finance records can be
/// explicitly shared between them. Individual data stays private unless marked
/// <see cref="Visibility.HouseholdShared"/>.
/// </summary>
public class Household : BaseEntity
{
    /// <summary>
    /// Display name of the household (e.g. "Andersson family").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Internal <see cref="User.Id"/> of the user who created the household.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    // Navigation properties
    /// <summary>
    /// Members belonging to this household.
    /// </summary>
    public ICollection<HouseholdMember> Members { get; set; } = new List<HouseholdMember>();

    /// <summary>
    /// Invites issued for this household.
    /// </summary>
    public ICollection<HouseholdInvite> Invites { get; set; } = new List<HouseholdInvite>();
}
