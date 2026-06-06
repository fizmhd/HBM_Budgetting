namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// The kind of period a budget covers. The MVP UI offers <see cref="Month"/> and
/// <see cref="CustomRange"/>; richer cycles (year/trip/salary-cycle) arrive later (D6). The entity
/// already stores explicit start/end dates, so extending the set is configuration, not schema change.
/// </summary>
public enum BudgetPeriodType
{
    /// <summary>A single calendar month (PeriodStart = first day, PeriodEnd = last day).</summary>
    Month = 0,

    /// <summary>An arbitrary inclusive date range chosen by the user.</summary>
    CustomRange = 1
}
