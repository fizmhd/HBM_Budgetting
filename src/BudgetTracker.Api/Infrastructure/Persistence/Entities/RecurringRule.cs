namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// A rule that repeatedly generates an income/expense transaction (R6). Owned by a user and optionally
/// shared with their household (via <see cref="OwnedEntity.Visibility"/>). The generation engine
/// (TASK 5.2) materialises <see cref="RecurringOccurrence"/>s as each <see cref="NextDueDate"/> arrives.
/// </summary>
public class RecurringRule : OwnedEntity
{
    /// <summary>Display name (e.g. "Salary", "Netflix").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Income or Expense. Transfers are not supported as recurring rules.</summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Account the generated transaction posts to. Optional — null mirrors account-less ("cash")
    /// transactions (TASK 4.1).
    /// </summary>
    public Guid? AccountId { get; set; }

    /// <summary>Category for the generated transaction's single split. Required to post a transaction.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Amount of each generated transaction, positive.</summary>
    public decimal Amount { get; set; }

    /// <summary>ISO currency code; "SEK" in the MVP.</summary>
    public string CurrencyCode { get; set; } = "SEK";

    public RecurrenceFrequency Frequency { get; set; }

    /// <summary>Repeat every <see cref="Interval"/> units of <see cref="Frequency"/> (e.g. 2 = bi-weekly).</summary>
    public int Interval { get; set; } = 1;

    /// <summary>
    /// For monthly rules, the day of month to post on (clamped to the month length). Null = use the
    /// start date's day.
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>First date the rule is due.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Optional last date; the rule stops generating after it.</summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>The next date an occurrence is due. Advanced by the engine after each generation.</summary>
    public DateOnly NextDueDate { get; set; }

    public GenerationMode GenerationMode { get; set; }

    public RecurringStatus Status { get; set; } = RecurringStatus.Active;

    /// <summary>True to surface under the "subscriptions" filter on the Repeating Payments page.</summary>
    public bool IsSubscription { get; set; }

    /// <summary>UTC time the rule was last paused (null when active / never paused).</summary>
    public DateTime? PausedAt { get; set; }

    /// <summary>UTC time the rule was last resumed.</summary>
    public DateTime? ResumedAt { get; set; }

    /// <summary>Occurrences generated from this rule.</summary>
    public List<RecurringOccurrence> Occurrences { get; set; } = new();
}
