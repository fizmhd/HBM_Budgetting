using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services.Budgets;

namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// A budget paired with its freshly-computed progress, fed to the alert service.
/// </summary>
public sealed record BudgetEvaluation(Budget Budget, BudgetProgress Progress);

/// <summary>
/// Raises budget threshold alerts (TASK 6.3): when a budget's usage first crosses its alert threshold
/// (or 100%), send one email and remember the crossing so later computes don't re-send.
/// </summary>
public interface IBudgetAlertService
{
    /// <summary>
    /// Processes the evaluated budgets, sending an email for each newly-crossed threshold and mutating
    /// <see cref="Budget.LastAlertedThreshold"/> accordingly. Returns true when at least one budget's
    /// alert state changed, so the caller knows to persist. Does not call SaveChanges itself.
    /// </summary>
    Task<bool> ProcessAsync(IReadOnlyCollection<BudgetEvaluation> evaluations,
        Guid userId, Guid? householdId, CancellationToken cancellationToken = default);
}
