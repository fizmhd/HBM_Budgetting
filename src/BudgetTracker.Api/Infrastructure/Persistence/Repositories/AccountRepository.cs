using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Account-specific operations.
/// </summary>
public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<Account>> GetVisibleAsync(Guid userId, Guid? householdId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .VisibleTo(userId, householdId)
            .OrderBy(a => a.IsArchived)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }
}
