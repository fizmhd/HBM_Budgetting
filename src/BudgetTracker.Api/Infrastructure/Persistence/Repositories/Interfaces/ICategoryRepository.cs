using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Category-specific operations.
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Loads every category visible to the caller (own + household-shared) in one query, ordered for
    /// display. The tree is built in memory from this flat list — fine for MVP volumes.
    /// </summary>
    Task<List<Category>> GetVisibleAsync(Guid userId, Guid? householdId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if the caller already has at least one category in their scope (own or household-shared).
    /// Used to decide whether to offer the default-taxonomy import.
    /// </summary>
    Task<bool> HasAnyAsync(Guid userId, Guid? householdId, CancellationToken cancellationToken = default);
}
