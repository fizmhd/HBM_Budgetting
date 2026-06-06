namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// One materialised instance of a <see cref="RecurringRule"/> for a specific due date (TASK 5.1).
/// Uniqueness on (rule, due date) makes generation idempotent — the engine never creates two
/// occurrences for the same date.
/// </summary>
public class RecurringOccurrence : BaseEntity
{
    public Guid RecurringRuleId { get; set; }

    /// <summary>The date this occurrence is for.</summary>
    public DateOnly DueDate { get; set; }

    public OccurrenceStatus Status { get; set; }

    /// <summary>Required when <see cref="Status"/> is Skipped.</summary>
    public string? SkipReason { get; set; }

    /// <summary>The transaction created for this occurrence (Posted), if any.</summary>
    public Guid? GeneratedTransactionId { get; set; }
}
