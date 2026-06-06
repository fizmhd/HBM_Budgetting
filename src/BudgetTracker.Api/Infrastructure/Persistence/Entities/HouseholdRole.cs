namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Role of a member within a household.
/// </summary>
/// <remarks>
/// Deliberately extensible: a parent-managed <c>Child</c> role is planned for a post-MVP stage,
/// so adding it later is an additive change rather than a redesign (see TASK 1.2 "Deferred").
/// </remarks>
public enum HouseholdRole
{
    /// <summary>
    /// The member who created the household. Can invite and remove members.
    /// </summary>
    Owner = 0,

    /// <summary>
    /// An adult member who joined via invite (e.g. a spouse).
    /// </summary>
    Member = 1
}
