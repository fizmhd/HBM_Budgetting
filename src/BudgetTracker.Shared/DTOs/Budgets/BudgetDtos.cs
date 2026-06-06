namespace BudgetTracker.Shared.DTOs.Budgets;

/// <summary>
/// A budget visible to the caller, with its live progress for the period (TASK 6.4).
/// </summary>
public class BudgetDto
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    /// <summary>Resolved category name, when available.</summary>
    public string? CategoryName { get; set; }

    /// <summary>"Month" or "CustomRange".</summary>
    public string PeriodType { get; set; } = "Month";

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    /// <summary>The spending limit for the period.</summary>
    public decimal Amount { get; set; }

    public int AlertThresholdPercent { get; set; }

    // ---- Derived progress (computed server-side) ----

    /// <summary>Total spent against the category in the period.</summary>
    public decimal Spent { get; set; }

    /// <summary>Limit minus spent (negative when exceeded).</summary>
    public decimal Remaining { get; set; }

    /// <summary>Spent as a percentage of the limit, rounded to 2 dp.</summary>
    public decimal PercentUsed { get; set; }

    /// <summary>"Ok", "Warning", or "Exceeded".</summary>
    public string Status { get; set; } = "Ok";

    /// <summary>True when shared with the household; otherwise individual/private.</summary>
    public bool IsShared { get; set; }
}

/// <summary>
/// Request to create a budget. For a Month period the caller supplies the month's first/last day as
/// <see cref="PeriodStart"/>/<see cref="PeriodEnd"/>.
/// </summary>
public class CreateBudgetRequest
{
    public Guid CategoryId { get; set; }

    /// <summary>"Month" or "CustomRange".</summary>
    public string PeriodType { get; set; } = "Month";

    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Percent-used at which to alert. Defaults to 80.</summary>
    public int AlertThresholdPercent { get; set; } = 80;

    /// <summary>Share the budget with the household instead of keeping it individual.</summary>
    public bool IsShared { get; set; }
}

/// <summary>
/// Request to update a budget. Same shape as create.
/// </summary>
public class UpdateBudgetRequest : CreateBudgetRequest
{
}
