using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Household-specific operations.
/// </summary>
public class HouseholdRepository : Repository<Household>, IHouseholdRepository
{
    public HouseholdRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<Household?> GetWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(h => h.Members)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }
}
