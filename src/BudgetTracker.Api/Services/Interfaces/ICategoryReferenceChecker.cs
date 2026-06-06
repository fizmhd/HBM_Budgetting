namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// Abstraction over the "is this category referenced by data that would orphan?" check used by the
/// deletion rule. Backed by transaction splits (Sprint 4) and budgets (Sprint 6); kept as an
/// interface so the <see cref="ICategoryService"/> rules can be unit-tested in isolation.
/// </summary>
public interface ICategoryReferenceChecker
{
    /// <summary>
    /// True when at least one transaction split or budget points at the given category.
    /// </summary>
    Task<bool> IsReferencedAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
