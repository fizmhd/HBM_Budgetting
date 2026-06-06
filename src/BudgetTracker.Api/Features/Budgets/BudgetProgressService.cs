using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Budgets;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Budgets;

namespace BudgetTracker.Api.Features.Budgets;

/// <summary>
/// Computes live progress for a set of budgets and runs threshold alerts (TASK 6.2 / 6.3). Shared by
/// the create/get/list slices so the spent-vs-budget rules live in exactly one place. The caller
/// persists when <see cref="ComputeAsync"/> reports that alert state changed.
/// </summary>
public sealed class BudgetProgressService
{
    private readonly ITransactionRepository _transactions;
    private readonly ICategoryRepository _categories;
    private readonly IBudgetAlertService _alerts;

    public BudgetProgressService(
        ITransactionRepository transactions,
        ICategoryRepository categories,
        IBudgetAlertService alerts)
    {
        _transactions = transactions;
        _categories = categories;
        _alerts = alerts;
    }

    /// <summary>
    /// The result of computing progress: the DTOs to return, and whether any budget's alert state was
    /// mutated (so the endpoint knows to call SaveChanges).
    /// </summary>
    public sealed record Result(List<BudgetDto> Budgets, bool AlertStateChanged);

    /// <summary>
    /// Computes spent/remaining/percent/status for each budget over its own period, raises any newly
    /// crossed alerts, and maps to DTOs (ordered as supplied).
    /// </summary>
    public async Task<Result> ComputeAsync(IReadOnlyCollection<Budget> budgets, Guid userId,
        Guid? householdId, CancellationToken ct)
    {
        var categoryNames = (await _categories.GetVisibleAsync(userId, householdId, ct))
            .ToDictionary(c => c.Id, c => c.Name);

        var progressByBudget = new Dictionary<Guid, BudgetProgress>();

        // Spent is scoped to each budget's own period, so group by period and issue one spend query
        // per distinct window (most budgets share the current month).
        foreach (var periodGroup in budgets.GroupBy(b => (b.PeriodStart, b.PeriodEnd)))
        {
            var categoryIds = periodGroup.Select(b => b.CategoryId).Distinct().ToList();
            var spentByCategory = await _transactions.GetSpentByCategoryAsync(
                userId, householdId, categoryIds, periodGroup.Key.PeriodStart, periodGroup.Key.PeriodEnd, ct);

            foreach (var budget in periodGroup)
            {
                var spent = spentByCategory.GetValueOrDefault(budget.CategoryId);
                progressByBudget[budget.Id] = BudgetCalculator.Evaluate(budget.Amount, spent, budget.AlertThresholdPercent);
            }
        }

        var evaluations = budgets
            .Select(b => new BudgetEvaluation(b, progressByBudget[b.Id]))
            .ToList();

        var alertStateChanged = await _alerts.ProcessAsync(evaluations, userId, householdId, ct);

        var dtos = budgets
            .Select(b => b.ToDto(progressByBudget[b.Id], categoryNames.GetValueOrDefault(b.CategoryId)))
            .ToList();

        return new Result(dtos, alertStateChanged);
    }

    /// <summary>
    /// Convenience overload for a single budget.
    /// </summary>
    public async Task<Result> ComputeAsync(Budget budget, Guid userId, Guid? householdId, CancellationToken ct) =>
        await ComputeAsync(new[] { budget }, userId, householdId, ct);
}
