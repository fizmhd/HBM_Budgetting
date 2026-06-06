using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Budgets;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Features.Budgets;

/// <summary>
/// Shared create/update logic for budgets: resolves and authorises the category, enforces the
/// scope/period rules, and mutates the entity. Used by both the create and update slices so the rules
/// live in one place. Structural shape (amount &gt; 0, start ≤ end, threshold range) is checked by the
/// FluentValidation validators; this layer covers the rules that need repository lookups.
/// </summary>
public sealed class BudgetWriteService
{
    public const string CategoryNotFoundCode = "BUDGET_CATEGORY_NOT_FOUND";
    public const string SharedRequiresHouseholdCode = "BUDGET_SHARED_REQUIRES_HOUSEHOLD";
    public const string PeriodTypeInvalidCode = "BUDGET_PERIOD_TYPE_INVALID";

    private readonly ICategoryRepository _categories;

    public BudgetWriteService(ICategoryRepository categories)
    {
        _categories = categories;
    }

    /// <summary>
    /// Applies <paramref name="req"/> onto <paramref name="budget"/> (fresh or existing). Caller
    /// persists on success. Resets the alert marker so progress re-evaluates against the new settings.
    /// </summary>
    public async Task<Result> ApplyAsync(Budget budget, CreateBudgetRequest req, Guid userId,
        Guid? householdId, CancellationToken ct)
    {
        if (!Enum.TryParse<BudgetPeriodType>(req.PeriodType, ignoreCase: true, out var periodType) ||
            !Enum.IsDefined(periodType))
        {
            return Result.Failure(Error.Validation(PeriodTypeInvalidCode, "Period type must be Month or CustomRange."));
        }

        if (req.IsShared && householdId is null)
        {
            return Result.Failure(Error.Validation(SharedRequiresHouseholdCode,
                "You must belong to a household to share a budget."));
        }

        // The category must be visible to the caller.
        var visibleCategoryIds = (await _categories.GetVisibleAsync(userId, householdId, ct))
            .Select(c => c.Id)
            .ToHashSet();
        if (!visibleCategoryIds.Contains(req.CategoryId))
        {
            return Result.Failure(Error.Validation(CategoryNotFoundCode, "Category not found."));
        }

        budget.OwnerUserId = userId;
        budget.Visibility = req.IsShared ? Visibility.HouseholdShared : Visibility.Individual;
        budget.HouseholdId = req.IsShared ? householdId : null;
        budget.CategoryId = req.CategoryId;
        budget.PeriodType = periodType;
        budget.PeriodStart = req.PeriodStart;
        budget.PeriodEnd = req.PeriodEnd;
        budget.Amount = req.Amount;
        budget.AlertThresholdPercent = req.AlertThresholdPercent;

        // Settings changed → re-arm alerts so the next compute can re-alert against the new limit.
        budget.LastAlertedThreshold = 0;

        return Result.Success();
    }
}
