using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Account-specific operations.
/// </summary>
public interface IAccountRepository : IRepository<Account>
{
    /// <summary>
    /// Lists the accounts visible to a user (own + household-shared), ordered for display.
    /// </summary>
    Task<List<Account>> GetVisibleAsync(Guid userId, Guid? householdId, CancellationToken cancellationToken = default);
}
