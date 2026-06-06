using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for RecurringRule-specific operations.
/// </summary>
public interface IRecurringRuleRepository : IRepository<RecurringRule>
{
    /// <summary>
    /// Lists the rules visible to the caller (own + household-shared), newest-due first.
    /// </summary>
    Task<List<RecurringRule>> GetVisibleAsync(Guid userId, Guid? householdId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the active rules that are due on or before <paramref name="asOf"/> (Status = Active,
    /// NextDueDate &lt;= asOf, and the rule has not passed its end date). Used by the generation engine
    /// across all owners; <paramref name="ownerFilter"/> restricts to one owner (manual "generate now").
    /// </summary>
    Task<List<RecurringRule>> GetDueAsync(DateOnly asOf, Guid? ownerFilter,
        CancellationToken cancellationToken = default);

    /// <summary>True if any rule references the given category. Backs the category-deletion rule.</summary>
    Task<bool> AnyForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
