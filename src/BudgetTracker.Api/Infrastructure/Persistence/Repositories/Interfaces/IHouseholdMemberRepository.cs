using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for HouseholdMember-specific operations.
/// </summary>
public interface IHouseholdMemberRepository : IRepository<HouseholdMember>
{
    /// <summary>
    /// Gets the membership for a user. In the MVP a user belongs to at most one household.
    /// </summary>
    Task<HouseholdMember?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all members of a household.
    /// </summary>
    Task<List<HouseholdMember>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if the user is a member of the given household.
    /// </summary>
    Task<bool> IsMemberAsync(Guid householdId, Guid userId, CancellationToken cancellationToken = default);
}
