using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for HouseholdInvite-specific operations.
/// </summary>
public class HouseholdInviteRepository : Repository<HouseholdInvite>, IHouseholdInviteRepository
{
    public HouseholdInviteRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<HouseholdInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.Household)
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);
    }
}
