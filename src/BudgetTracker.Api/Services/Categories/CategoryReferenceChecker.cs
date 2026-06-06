using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services.Categories;

/// <summary>
/// Default <see cref="ICategoryReferenceChecker"/>: a category is "in use" when a transaction split
/// (Sprint 4), a budget (Sprint 6), or a recurring rule (Sprint 5) points at it.
/// </summary>
public sealed class CategoryReferenceChecker : ICategoryReferenceChecker
{
    private readonly ITransactionRepository _transactions;
    private readonly IBudgetRepository _budgets;
    private readonly IRecurringRuleRepository _recurringRules;

    public CategoryReferenceChecker(ITransactionRepository transactions, IBudgetRepository budgets,
        IRecurringRuleRepository recurringRules)
    {
        _transactions = transactions;
        _budgets = budgets;
        _recurringRules = recurringRules;
    }

    /// <inheritdoc />
    public async Task<bool> IsReferencedAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _transactions.AnySplitForCategoryAsync(categoryId, cancellationToken)
            || await _budgets.AnyForCategoryAsync(categoryId, cancellationToken)
            || await _recurringRules.AnyForCategoryAsync(categoryId, cancellationToken);
    }
}
