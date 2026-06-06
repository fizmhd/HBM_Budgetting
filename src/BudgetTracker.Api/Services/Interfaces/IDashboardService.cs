using BudgetTracker.Shared.DTOs.Dashboard;

namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// Builds the at-a-glance monthly dashboard (TASK 7.1 / R8) by aggregating transactions, budgets and
/// account balances server-side, respecting the requested visibility scope.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Aggregates the dashboard for the given month. <paramref name="householdScope"/> true includes
    /// everything visible to the caller (own + household-shared); false restricts the income/expense
    /// figures to the caller's own records.
    /// </summary>
    Task<MonthlyDashboardDto> BuildMonthlyAsync(Guid userId, Guid? householdId, bool householdScope,
        int year, int month, CancellationToken cancellationToken = default);
}
