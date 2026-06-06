using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// Computes live account balances from transactions (TASK 4.4):
/// <c>CurrentBalance = OpeningBalance + Σ(income) − Σ(expense) ± transfers</c>.
/// </summary>
public interface IBalanceService
{
    /// <summary>
    /// Returns the current balance for each supplied account, derived from the transactions visible to
    /// the caller. Accounts with no transactions return their opening balance.
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetBalancesAsync(Guid userId, Guid? householdId,
        IEnumerable<Account> accounts, CancellationToken cancellationToken = default);
}
