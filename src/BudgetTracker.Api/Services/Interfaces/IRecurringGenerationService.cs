using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// The recurring generation engine (TASK 5.2): turns due rules into occurrences (and, for AutoPost,
/// transactions), idempotently. Also posts the transaction for a single occurrence (used by AutoPost
/// generation and by manual confirm of a PendingConfirm occurrence).
/// </summary>
public interface IRecurringGenerationService
{
    /// <summary>
    /// Generates occurrences for every active rule due on or before <paramref name="asOf"/>, catching
    /// up missed periods and advancing each rule's next-due date, stopping at the end date. Idempotent —
    /// re-running never double-generates the same (rule, date). <paramref name="ownerFilter"/> limits to
    /// one owner (manual "generate now"); null processes all owners (the scheduled job). Persists once.
    /// Returns the number of occurrences created.
    /// </summary>
    Task<int> GenerateDueAsync(DateOnly asOf, Guid? ownerFilter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds and stages (does not save) the transaction for one due date of a rule, applying the same
    /// invariants as manual transaction entry. Returns the new transaction id on success.
    /// </summary>
    Task<Result<Guid>> PostTransactionAsync(RecurringRule rule, DateOnly dueDate, Guid? householdId,
        CancellationToken cancellationToken = default);
}
