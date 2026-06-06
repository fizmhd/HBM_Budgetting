namespace BudgetTracker.Shared.DTOs.Dashboard;

/// <summary>
/// Expense total for one category in the dashboard period.
/// </summary>
public class DashboardCategorySpendDto
{
    public Guid CategoryId { get; set; }
    public string? Name { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Share of total expenses (0–100), for rendering a bar without a chart library.</summary>
    public decimal Percent { get; set; }
}

/// <summary>
/// A roll-up of the budgets active in the period (TASK 6 surfaced on the dashboard).
/// </summary>
public class DashboardBudgetSummaryDto
{
    public int Total { get; set; }
    public int OnTrack { get; set; }
    public int Warning { get; set; }
    public int Exceeded { get; set; }
    public decimal TotalBudgeted { get; set; }
    public decimal TotalSpent { get; set; }
}

/// <summary>
/// An account with its current balance, for the dashboard's account snapshot.
/// </summary>
public class DashboardAccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "SEK";
    public decimal Balance { get; set; }
}

/// <summary>
/// The at-a-glance monthly picture (TASK 7.1 / R8). Income/expenses/net and the category breakdown
/// respect the requested scope (individual = the caller's own records; household = everything visible
/// to the caller). The budget and account snapshots reflect everything visible to the caller.
/// </summary>
public class MonthlyDashboardDto
{
    /// <summary>The period, "yyyy-MM".</summary>
    public string Month { get; set; } = string.Empty;

    /// <summary>"individual" or "household".</summary>
    public string Scope { get; set; } = "household";

    /// <summary>Base currency of the figures (SEK in the MVP).</summary>
    public string CurrencyCode { get; set; } = "SEK";

    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }

    /// <summary>Income minus expenses for the period.</summary>
    public decimal NetBalance { get; set; }

    /// <summary>Expense totals per category, largest first.</summary>
    public List<DashboardCategorySpendDto> ByCategory { get; set; } = new();

    public DashboardBudgetSummaryDto BudgetSummary { get; set; } = new();

    /// <summary>Top accounts by balance (visible to the caller).</summary>
    public List<DashboardAccountDto> TopAccounts { get; set; } = new();
}
