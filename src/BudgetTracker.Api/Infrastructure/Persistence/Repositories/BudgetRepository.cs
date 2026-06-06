using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Budget-specific operations.
/// </summary>
public class BudgetRepository : Repository<Budget>, IBudgetRepository
{
    public BudgetRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<Budget>> GetVisibleAsync(Guid userId, Guid? householdId, DateOnly? overlapFrom,
        DateOnly? overlapTo, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.VisibleTo(userId, householdId);

        // Overlap test: a budget [start,end] overlaps the window [from,to] when start <= to && end >= from.
        if (overlapTo is { } to)
        {
            query = query.Where(b => b.PeriodStart <= to);
        }
        if (overlapFrom is { } from)
        {
            query = query.Where(b => b.PeriodEnd >= from);
        }

        return await query
            .OrderByDescending(b => b.PeriodStart)
            .ThenByDescending(b => b.Amount)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(b => b.CategoryId == categoryId, cancellationToken);
    }
}
