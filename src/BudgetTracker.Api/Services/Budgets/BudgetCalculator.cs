namespace BudgetTracker.Api.Services.Budgets;

/// <summary>
/// Status of a budget relative to its limit (TASK 6.2 / 6.3).
/// </summary>
public enum BudgetStatus
{
    /// <summary>Under the alert threshold.</summary>
    Ok = 0,

    /// <summary>At/above the alert threshold but not yet over the limit.</summary>
    Warning = 1,

    /// <summary>Spending has reached or passed the limit.</summary>
    Exceeded = 2
}

/// <summary>
/// The derived progress of a budget for a period.
/// </summary>
/// <param name="Spent">Total spent against the budget's category in the period.</param>
/// <param name="Remaining">Limit minus spent (can be negative when exceeded).</param>
/// <param name="PercentUsed">Spent as a percentage of the limit, rounded to 2 dp.</param>
/// <param name="Status">OK / Warning / Exceeded.</param>
public readonly record struct BudgetProgress(decimal Spent, decimal Remaining, decimal PercentUsed, BudgetStatus Status);

/// <summary>
/// Pure spent-vs-budget computation (TASK 6.2). Kept free of I/O so it is exhaustively unit-testable
/// around the threshold and 100% boundaries.
/// </summary>
public static class BudgetCalculator
{
    /// <summary>
    /// Evaluates progress for a limit of <paramref name="amount"/> with <paramref name="spent"/>
    /// already spent and an alert threshold of <paramref name="alertThresholdPercent"/> percent.
    /// Status uses the exact ratio (not the rounded percent) so a value just under the threshold does
    /// not tip into Warning. Reaching the limit exactly is Exceeded; reaching the threshold exactly is
    /// Warning.
    /// </summary>
    public static BudgetProgress Evaluate(decimal amount, decimal spent, int alertThresholdPercent)
    {
        var remaining = amount - spent;
        var ratio = amount > 0 ? spent / amount * 100m : (spent > 0 ? 100m : 0m);
        var percentUsed = Math.Round(ratio, 2, MidpointRounding.AwayFromZero);

        BudgetStatus status;
        if (amount > 0 ? spent >= amount : spent > 0)
        {
            status = BudgetStatus.Exceeded;
        }
        else if (ratio >= alertThresholdPercent)
        {
            status = BudgetStatus.Warning;
        }
        else
        {
            status = BudgetStatus.Ok;
        }

        return new BudgetProgress(spent, remaining, percentUsed, status);
    }

    /// <summary>
    /// The alert level a progress represents: 100 when exceeded, the budget's threshold when warning,
    /// 0 when OK. Used to send a single email per threshold crossing (TASK 6.3).
    /// </summary>
    public static int AlertLevel(BudgetProgress progress, int alertThresholdPercent) => progress.Status switch
    {
        BudgetStatus.Exceeded => 100,
        BudgetStatus.Warning => alertThresholdPercent,
        _ => 0
    };
}
