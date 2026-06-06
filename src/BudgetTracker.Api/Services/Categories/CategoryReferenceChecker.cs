using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services.Categories;

/// <summary>
/// Default <see cref="ICategoryReferenceChecker"/>: a category is "in use" when a transaction split
/// points at it. Budget references are wired in Sprint 6 (no budget table exists yet).
/// </summary>
public sealed class CategoryReferenceChecker : ICategoryReferenceChecker
{
    private readonly ITransactionRepository _transactions;

    public CategoryReferenceChecker(ITransactionRepository transactions)
    {
        _transactions = transactions;
    }

    /// <inheritdoc />
    public Task<bool> IsReferencedAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return _transactions.AnySplitForCategoryAsync(categoryId, cancellationToken);
    }
}
