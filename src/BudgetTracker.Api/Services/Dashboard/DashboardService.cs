using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Budgets;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Dashboard;

namespace BudgetTracker.Api.Services.Dashboard;

/// <summary>
/// Default <see cref="IDashboardService"/> (TASK 7.1). Income/expense/net and the category breakdown
/// honour the requested scope; the budget and account snapshots use the caller's full visible set
/// (budgets and balances are inherently shared concepts). All figures are in the base currency (SEK).
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly ITransactionRepository _transactions;
    private readonly IBudgetRepository _budgets;
    private readonly IAccountRepository _accounts;
    private readonly ICategoryRepository _categories;
    private readonly IBalanceService _balances;

    private const int TopAccountCount = 5;

    public DashboardService(
        ITransactionRepository transactions,
        IBudgetRepository budgets,
        IAccountRepository accounts,
        ICategoryRepository categories,
        IBalanceService balances)
    {
        _transactions = transactions;
        _budgets = budgets;
        _accounts = accounts;
        _categories = categories;
        _balances = balances;
    }

    /// <inheritdoc />
    public async Task<MonthlyDashboardDto> BuildMonthlyAsync(Guid userId, Guid? householdId,
        bool householdScope, int year, int month, CancellationToken cancellationToken = default)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);

        var categoryNames = (await _categories.GetVisibleAsync(userId, householdId, cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);

        var (income, expenses) = await _transactions.GetMonthlyTotalsAsync(
            userId, householdId, householdScope, from, to, cancellationToken);

        var byCategory = await BuildByCategoryAsync(userId, householdId, householdScope, from, to,
            categoryNames, cancellationToken);

        var budgetSummary = await BuildBudgetSummaryAsync(userId, householdId, from, to, cancellationToken);
        var topAccounts = await BuildTopAccountsAsync(userId, householdId, cancellationToken);

        return new MonthlyDashboardDto
        {
            Month = $"{year:D4}-{month:D2}",
            Scope = householdScope ? "household" : "individual",
            CurrencyCode = "SEK",
            TotalIncome = income,
            TotalExpenses = expenses,
            NetBalance = income - expenses,
            ByCategory = byCategory,
            BudgetSummary = budgetSummary,
            TopAccounts = topAccounts
        };
    }

    private async Task<List<DashboardCategorySpendDto>> BuildByCategoryAsync(Guid userId, Guid? householdId,
        bool householdScope, DateOnly from, DateOnly to, IReadOnlyDictionary<Guid, string> categoryNames,
        CancellationToken ct)
    {
        var rows = await _transactions.GetExpenseByCategoryAsync(userId, householdId, householdScope, from, to, ct);
        var total = rows.Sum(r => r.Amount);

        return rows
            .Select(r => new DashboardCategorySpendDto
            {
                CategoryId = r.CategoryId,
                Name = categoryNames.GetValueOrDefault(r.CategoryId),
                Amount = r.Amount,
                Percent = total > 0 ? Math.Round(r.Amount / total * 100m, 1, MidpointRounding.AwayFromZero) : 0m
            })
            .OrderByDescending(c => c.Amount)
            .ToList();
    }

    private async Task<DashboardBudgetSummaryDto> BuildBudgetSummaryAsync(Guid userId, Guid? householdId,
        DateOnly from, DateOnly to, CancellationToken ct)
    {
        var budgets = await _budgets.GetVisibleAsync(userId, householdId, from, to, ct);
        var summary = new DashboardBudgetSummaryDto { Total = budgets.Count };

        // Spent is scoped to each budget's own period; group by period to minimise spend queries.
        foreach (var periodGroup in budgets.GroupBy(b => (b.PeriodStart, b.PeriodEnd)))
        {
            var categoryIds = periodGroup.Select(b => b.CategoryId).Distinct().ToList();
            var spentByCategory = await _transactions.GetSpentByCategoryAsync(
                userId, householdId, categoryIds, periodGroup.Key.PeriodStart, periodGroup.Key.PeriodEnd, ct);

            foreach (var budget in periodGroup)
            {
                var spent = spentByCategory.GetValueOrDefault(budget.CategoryId);
                var progress = BudgetCalculator.Evaluate(budget.Amount, spent, budget.AlertThresholdPercent);

                summary.TotalBudgeted += budget.Amount;
                summary.TotalSpent += spent;
                switch (progress.Status)
                {
                    case BudgetStatus.Exceeded: summary.Exceeded++; break;
                    case BudgetStatus.Warning: summary.Warning++; break;
                    default: summary.OnTrack++; break;
                }
            }
        }

        return summary;
    }

    private async Task<List<DashboardAccountDto>> BuildTopAccountsAsync(Guid userId, Guid? householdId,
        CancellationToken ct)
    {
        var accounts = (await _accounts.GetVisibleAsync(userId, householdId, ct))
            .Where(a => !a.IsArchived)
            .ToList();
        if (accounts.Count == 0)
        {
            return new List<DashboardAccountDto>();
        }

        var balances = await _balances.GetBalancesAsync(userId, householdId, accounts, ct);

        return accounts
            .Select(a => new DashboardAccountDto
            {
                Id = a.Id,
                Name = a.Name,
                CurrencyCode = a.CurrencyCode,
                Balance = balances.TryGetValue(a.Id, out var b) ? b : a.OpeningBalance
            })
            .OrderByDescending(a => a.Balance)
            .Take(TopAccountCount)
            .ToList();
    }
}
