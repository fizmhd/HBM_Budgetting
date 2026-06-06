using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Recurring;
using BudgetTracker.Shared.DTOs.Recurring;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Features.Recurring;

/// <summary>
/// Shared create/update logic for recurring rules: validates the type/scope, resolves and authorises
/// the account and category, and sets the fields (including the computed first/next due date). Used by
/// both the create and update slices. Structural shape (amount &gt; 0, interval, day-of-month range,
/// valid dates) is checked by the FluentValidation validators.
/// </summary>
public sealed class RecurringWriteService
{
    public const string TypeInvalidCode = "RECURRING_TYPE_INVALID";
    public const string FrequencyInvalidCode = "RECURRING_FREQUENCY_INVALID";
    public const string GenerationModeInvalidCode = "RECURRING_GENERATION_MODE_INVALID";
    public const string AccountNotFoundCode = "RECURRING_ACCOUNT_NOT_FOUND";
    public const string CategoryNotFoundCode = "RECURRING_CATEGORY_NOT_FOUND";
    public const string SharedRequiresHouseholdCode = "RECURRING_SHARED_REQUIRES_HOUSEHOLD";

    private readonly IAccountRepository _accounts;
    private readonly ICategoryRepository _categories;

    public RecurringWriteService(IAccountRepository accounts, ICategoryRepository categories)
    {
        _accounts = accounts;
        _categories = categories;
    }

    /// <summary>
    /// Applies <paramref name="req"/> onto <paramref name="rule"/> (fresh or existing). On any change to
    /// the schedule the next-due date is recomputed from the start date. Caller persists on success.
    /// </summary>
    public async Task<Result> ApplyAsync(RecurringRule rule, CreateRecurringRuleRequest req, Guid userId,
        Guid? householdId, CancellationToken ct)
    {
        if (!Enum.TryParse<TransactionType>(req.Type, ignoreCase: true, out var type) ||
            type is not (TransactionType.Income or TransactionType.Expense))
        {
            return Result.Failure(Error.Validation(TypeInvalidCode, "Type must be Income or Expense."));
        }

        if (!Enum.TryParse<RecurrenceFrequency>(req.Frequency, ignoreCase: true, out var frequency) ||
            !Enum.IsDefined(frequency))
        {
            return Result.Failure(Error.Validation(FrequencyInvalidCode,
                "Frequency must be Daily, Weekly, Monthly, or Yearly."));
        }

        if (!Enum.TryParse<GenerationMode>(req.GenerationMode, ignoreCase: true, out var generationMode) ||
            !Enum.IsDefined(generationMode))
        {
            return Result.Failure(Error.Validation(GenerationModeInvalidCode,
                "Generation mode must be AutoPost or PendingConfirm."));
        }

        if (req.IsShared && householdId is null)
        {
            return Result.Failure(Error.Validation(SharedRequiresHouseholdCode,
                "You must belong to a household to share a recurring rule."));
        }

        // Account (when supplied) must be visible.
        if (req.AccountId is { } accountId)
        {
            var account = await _accounts.GetByIdAsync(accountId, ct);
            if (account is null || !account.IsVisibleTo(userId, householdId))
            {
                return Result.Failure(Error.Validation(AccountNotFoundCode, "Account not found."));
            }
        }

        // Category is required to post a transaction, and must be visible.
        if (req.CategoryId is not { } categoryId)
        {
            return Result.Failure(Error.Validation(CategoryNotFoundCode, "A category is required."));
        }
        var visibleCategoryIds = (await _categories.GetVisibleAsync(userId, householdId, ct))
            .Select(c => c.Id)
            .ToHashSet();
        if (!visibleCategoryIds.Contains(categoryId))
        {
            return Result.Failure(Error.Validation(CategoryNotFoundCode, "Category not found."));
        }

        rule.OwnerUserId = userId;
        rule.Visibility = req.IsShared ? Visibility.HouseholdShared : Visibility.Individual;
        rule.HouseholdId = req.IsShared ? householdId : null;
        rule.Name = req.Name.Trim();
        rule.Type = type;
        rule.AccountId = req.AccountId;
        rule.CategoryId = categoryId;
        rule.Amount = req.Amount;
        rule.CurrencyCode = string.IsNullOrWhiteSpace(req.CurrencyCode) ? "SEK" : req.CurrencyCode.Trim().ToUpperInvariant();
        rule.Frequency = frequency;
        rule.Interval = Math.Max(1, req.Interval);
        rule.DayOfMonth = frequency == RecurrenceFrequency.Monthly ? req.DayOfMonth : null;
        rule.StartDate = req.StartDate;
        rule.EndDate = req.EndDate;
        rule.GenerationMode = generationMode;
        rule.IsSubscription = req.IsSubscription;

        // Recompute the next-due date from the (possibly changed) schedule.
        rule.NextDueDate = RecurrenceCalculator.FirstDueDate(req.StartDate, frequency, rule.DayOfMonth);

        return Result.Success();
    }
}
