using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.DTOs.Recurring;

namespace BudgetTracker.Api.Features.Recurring;

/// <summary>
/// Mapping helpers between recurring entities and DTOs.
/// </summary>
public static class RecurringMapping
{
    public static RecurringRuleDto ToDto(this RecurringRule rule,
        IReadOnlyDictionary<Guid, string> accountNames,
        IReadOnlyDictionary<Guid, string> categoryNames) => new()
    {
        Id = rule.Id,
        Name = rule.Name,
        Type = rule.Type.ToString(),
        AccountId = rule.AccountId,
        AccountName = rule.AccountId is { } a ? accountNames.GetValueOrDefault(a) : null,
        CategoryId = rule.CategoryId,
        CategoryName = rule.CategoryId is { } c ? categoryNames.GetValueOrDefault(c) : null,
        Amount = rule.Amount,
        CurrencyCode = rule.CurrencyCode,
        Frequency = rule.Frequency.ToString(),
        Interval = rule.Interval,
        DayOfMonth = rule.DayOfMonth,
        StartDate = rule.StartDate,
        EndDate = rule.EndDate,
        NextDueDate = rule.NextDueDate,
        GenerationMode = rule.GenerationMode.ToString(),
        Status = rule.Status.ToString(),
        IsSubscription = rule.IsSubscription,
        PausedAt = rule.PausedAt,
        ResumedAt = rule.ResumedAt,
        IsShared = rule.Visibility == Visibility.HouseholdShared
    };

    public static RecurringOccurrenceDto ToDto(this RecurringOccurrence occurrence, RecurringRule? rule = null) => new()
    {
        Id = occurrence.Id,
        RecurringRuleId = occurrence.RecurringRuleId,
        DueDate = occurrence.DueDate,
        Status = occurrence.Status.ToString(),
        SkipReason = occurrence.SkipReason,
        GeneratedTransactionId = occurrence.GeneratedTransactionId,
        RuleName = rule?.Name,
        RuleType = rule?.Type.ToString(),
        Amount = rule?.Amount ?? 0m,
        CurrencyCode = rule?.CurrencyCode ?? "SEK"
    };
}
