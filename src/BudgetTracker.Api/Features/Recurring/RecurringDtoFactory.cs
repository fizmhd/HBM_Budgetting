using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Recurring;

namespace BudgetTracker.Api.Features.Recurring;

/// <summary>
/// Builds a rule-to-DTO mapper for a caller, loading the account and category names visible to them
/// once. Keeps the name lookups out of every recurring read slice.
/// </summary>
public sealed class RecurringDtoFactory
{
    private readonly IAccountRepository _accounts;
    private readonly ICategoryRepository _categories;

    public RecurringDtoFactory(IAccountRepository accounts, ICategoryRepository categories)
    {
        _accounts = accounts;
        _categories = categories;
    }

    /// <summary>Returns a function that maps a rule to its DTO with resolved account/category names.</summary>
    public async Task<Func<RecurringRule, RecurringRuleDto>> CreateMapperAsync(Guid userId, Guid? householdId,
        CancellationToken ct)
    {
        var accountNames = (await _accounts.GetVisibleAsync(userId, householdId, ct))
            .ToDictionary(a => a.Id, a => a.Name);
        var categoryNames = (await _categories.GetVisibleAsync(userId, householdId, ct))
            .ToDictionary(c => c.Id, c => c.Name);

        return rule => rule.ToDto(accountNames, categoryNames);
    }
}
