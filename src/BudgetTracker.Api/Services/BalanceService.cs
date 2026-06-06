using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services;

/// <summary>
/// Derives live account balances by folding transaction effects onto opening balances (TASK 4.4).
/// </summary>
public sealed class BalanceService : IBalanceService
{
    private readonly ITransactionRepository _transactions;

    public BalanceService(ITransactionRepository transactions)
    {
        _transactions = transactions;
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, decimal>> GetBalancesAsync(Guid userId, Guid? householdId,
        IEnumerable<Account> accounts, CancellationToken cancellationToken = default)
    {
        var deltas = await _transactions.GetBalanceDeltasAsync(userId, householdId, cancellationToken);

        return accounts.ToDictionary(
            account => account.Id,
            account => account.OpeningBalance + (deltas.TryGetValue(account.Id, out var d) ? d : 0m));
    }
}
