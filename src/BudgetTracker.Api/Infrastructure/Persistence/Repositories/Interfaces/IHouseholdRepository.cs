using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Household-specific operations.
/// </summary>
public interface IHouseholdRepository : IRepository<Household>
{
    /// <summary>
    /// Gets a household with its members eagerly loaded.
    /// </summary>
    Task<Household?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default);
}
