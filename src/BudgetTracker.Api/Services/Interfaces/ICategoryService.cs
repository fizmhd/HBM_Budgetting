using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// Enforces the category management rules (R4): rename always allowed; move/re-parent allowed but
/// must not create a cycle; delete blocked while the category has children or is referenced by a
/// transaction split or budget.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Renames a category. Always allowed for a valid, non-empty name.
    /// </summary>
    Result Rename(Category category, string newName);

    /// <summary>
    /// Re-parents a category under <paramref name="newParentId"/> (null = make it a root), preventing
    /// cycles. <paramref name="scope"/> is the full set of categories visible in the caller's scope,
    /// used to walk the descendant chain.
    /// </summary>
    Result Move(Category category, Guid? newParentId, IReadOnlyCollection<Category> scope);

    /// <summary>
    /// Deletes a category if the rules allow it: it must have no children and not be referenced by any
    /// transaction split or budget; otherwise fails with <c>CATEGORY_IN_USE</c>.
    /// </summary>
    Task<Result> DeleteAsync(Category category, IReadOnlyCollection<Category> scope, CancellationToken cancellationToken = default);
}
