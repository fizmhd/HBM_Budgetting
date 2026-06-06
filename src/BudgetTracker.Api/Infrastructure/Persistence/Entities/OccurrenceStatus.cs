namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// State of a single generated occurrence of a recurring rule.
/// </summary>
public enum OccurrenceStatus
{
    /// <summary>Awaiting user confirmation (PendingConfirm rules); no transaction yet.</summary>
    Pending = 0,

    /// <summary>A transaction has been created for this occurrence.</summary>
    Posted = 1,

    /// <summary>The user skipped this occurrence (with a reason); no transaction.</summary>
    Skipped = 2
}
