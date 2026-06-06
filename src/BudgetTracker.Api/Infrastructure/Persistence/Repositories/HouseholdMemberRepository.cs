using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for HouseholdMember-specific operations.
/// </summary>
public class HouseholdMemberRepository : Repository<HouseholdMember>, IHouseholdMemberRepository
{
    public HouseholdMemberRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<HouseholdMember?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<HouseholdMember>> GetByHouseholdIdAsync(Guid householdId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(m => m.HouseholdId == householdId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsMemberAsync(Guid householdId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(m => m.HouseholdId == householdId && m.UserId == userId, cancellationToken);
    }
}
