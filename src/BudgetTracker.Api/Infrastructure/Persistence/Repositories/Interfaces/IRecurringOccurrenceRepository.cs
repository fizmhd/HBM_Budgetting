using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for RecurringOccurrence-specific operations.
/// </summary>
public interface IRecurringOccurrenceRepository : IRepository<RecurringOccurrence>
{
    /// <summary>True if an occurrence already exists for the rule on that date (idempotent generation).</summary>
    Task<bool> ExistsAsync(Guid recurringRuleId, DateOnly dueDate, CancellationToken cancellationToken = default);

    /// <summary>All occurrences for a rule, newest due first.</summary>
    Task<List<RecurringOccurrence>> GetByRuleAsync(Guid recurringRuleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pending occurrences belonging to rules visible to the caller, with their owning rule, oldest due
    /// first — the confirmation queue for the UI.
    /// </summary>
    Task<List<(RecurringOccurrence Occurrence, RecurringRule Rule)>> GetPendingVisibleAsync(
        Guid userId, Guid? householdId, CancellationToken cancellationToken = default);
}
