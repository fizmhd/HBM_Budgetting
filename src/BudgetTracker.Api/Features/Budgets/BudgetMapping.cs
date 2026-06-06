using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services.Budgets;
using BudgetTracker.Shared.DTOs.Budgets;

namespace BudgetTracker.Api.Features.Budgets;

/// <summary>
/// Mapping helpers between Budget entities (+ computed progress) and DTOs.
/// </summary>
public static class BudgetMapping
{
    public static BudgetDto ToDto(this Budget budget, BudgetProgress progress, string? categoryName) => new()
    {
        Id = budget.Id,
        CategoryId = budget.CategoryId,
        CategoryName = categoryName,
        PeriodType = budget.PeriodType.ToString(),
        PeriodStart = budget.PeriodStart,
        PeriodEnd = budget.PeriodEnd,
        Amount = budget.Amount,
        AlertThresholdPercent = budget.AlertThresholdPercent,
        Spent = progress.Spent,
        Remaining = progress.Remaining,
        PercentUsed = progress.PercentUsed,
        Status = progress.Status.ToString(),
        IsShared = budget.Visibility == Visibility.HouseholdShared
    };
}
