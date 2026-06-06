namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// A single money movement: income, expense, or transfer. Owned by a user and optionally shared with
/// their household (via <see cref="OwnedEntity.Visibility"/>). Income/expense carry one or more
/// <see cref="TransactionSplit"/>s whose amounts sum to <see cref="Amount"/>; transfers carry none.
/// </summary>
public class Transaction : OwnedEntity
{
    /// <summary>
    /// Account the transaction belongs to. For a transfer this is the source ("from").
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Calendar date the transaction occurred (no time component).
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Whether this is income, an expense, or a transfer.
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Gross amount, always positive. Direction is implied by <see cref="Type"/>.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ISO currency code. "SEK" in the MVP; stored so the schema is currency-ready.
    /// </summary>
    public string CurrencyCode { get; set; } = "SEK";

    /// <summary>
    /// Short human label (e.g. payee).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Free-text notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Destination account for a transfer ("to"); null for income/expense.
    /// </summary>
    public Guid? CounterAccountId { get; set; }

    /// <summary>
    /// Category splits for income/expense. Empty for transfers.
    /// </summary>
    public List<TransactionSplit> Splits { get; set; } = new();

    /// <summary>
    /// Tag join rows attached to this transaction.
    /// </summary>
    public List<TransactionTag> TransactionTags { get; set; } = new();
}
