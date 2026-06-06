namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Privacy scope for a domain record owned by a user.
/// </summary>
public enum Visibility
{
    /// <summary>
    /// Private to the owning user. The default scope for all data.
    /// </summary>
    Individual = 0,

    /// <summary>
    /// Shared with every member of the owner's household.
    /// </summary>
    HouseholdShared = 1,

    /// <summary>
    /// Reserved for Phase 2 (sharing with a specific group). Not used in the MVP.
    /// </summary>
    GroupShared = 2
}
