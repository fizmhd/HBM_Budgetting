namespace BudgetTracker.Shared.DTOs.Recurring;

/// <summary>
/// A generated occurrence of a recurring rule.
/// </summary>
public class RecurringOccurrenceDto
{
    public Guid Id { get; set; }
    public Guid RecurringRuleId { get; set; }
    public DateOnly DueDate { get; set; }

    /// <summary>"Pending", "Posted", or "Skipped".</summary>
    public string Status { get; set; } = string.Empty;

    public string? SkipReason { get; set; }
    public Guid? GeneratedTransactionId { get; set; }

    // Convenience fields for the confirmation queue (the owning rule's summary).
    public string? RuleName { get; set; }
    public string? RuleType { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "SEK";
}

/// <summary>
/// A recurring rule visible to the caller.
/// </summary>
public class RecurringRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>"Income" or "Expense".</summary>
    public string Type { get; set; } = "Expense";

    public Guid? AccountId { get; set; }
    public string? AccountName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "SEK";

    /// <summary>"Daily", "Weekly", "Monthly", "Yearly".</summary>
    public string Frequency { get; set; } = "Monthly";
    public int Interval { get; set; } = 1;
    public int? DayOfMonth { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly NextDueDate { get; set; }

    /// <summary>"AutoPost" or "PendingConfirm".</summary>
    public string GenerationMode { get; set; } = "AutoPost";

    /// <summary>"Active" or "Paused".</summary>
    public string Status { get; set; } = "Active";

    public bool IsSubscription { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? ResumedAt { get; set; }
    public bool IsShared { get; set; }
}

/// <summary>
/// Request to create a recurring rule.
/// </summary>
public class CreateRecurringRuleRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>"Income" or "Expense" (transfers are not supported).</summary>
    public string Type { get; set; } = "Expense";

    /// <summary>Optional account (null = account-less / "cash").</summary>
    public Guid? AccountId { get; set; }

    /// <summary>Category for the generated transaction (required to post).</summary>
    public Guid? CategoryId { get; set; }

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "SEK";

    public string Frequency { get; set; } = "Monthly";
    public int Interval { get; set; } = 1;
    public int? DayOfMonth { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string GenerationMode { get; set; } = "AutoPost";
    public bool IsSubscription { get; set; }
    public bool IsShared { get; set; }
}

/// <summary>
/// Request to update a recurring rule. Same shape as create.
/// </summary>
public class UpdateRecurringRuleRequest : CreateRecurringRuleRequest
{
}

/// <summary>
/// Request to skip a pending occurrence (reason required, TASK 5.3).
/// </summary>
public class SkipOccurrenceRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Result of a manual "generate now" run.
/// </summary>
public class RecurringGenerationResultDto
{
    /// <summary>Number of occurrences created.</summary>
    public int Generated { get; set; }
}
