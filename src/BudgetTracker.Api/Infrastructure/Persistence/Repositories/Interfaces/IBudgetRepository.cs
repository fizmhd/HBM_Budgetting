using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Budget-specific operations.
/// </summary>
public interface IBudgetRepository : IRepository<Budget>
{
    /// <summary>
    /// Loads every budget visible to the caller (own + household-shared), optionally restricted to
    /// budgets whose period overlaps the given window. Ordered for display (period start, then amount).
    /// </summary>
    Task<List<Budget>> GetVisibleAsync(Guid userId, Guid? householdId, DateOnly? overlapFrom,
        DateOnly? overlapTo, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if any budget references the given category. Backs the category-deletion rule (TASK 3.2).
    /// </summary>
    Task<bool> AnyForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
