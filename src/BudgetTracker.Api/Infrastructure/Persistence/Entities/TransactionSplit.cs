namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// One category allocation within a transaction. A single-category transaction has exactly one split;
/// a multi-category transaction has several whose amounts sum to the parent transaction's amount.
/// </summary>
public class TransactionSplit : BaseEntity
{
    /// <summary>
    /// Owning transaction.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Category this portion is booked against.
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Portion of the transaction amount allocated to <see cref="CategoryId"/>. Positive.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional per-split note.
    /// </summary>
    public string? Note { get; set; }
}
