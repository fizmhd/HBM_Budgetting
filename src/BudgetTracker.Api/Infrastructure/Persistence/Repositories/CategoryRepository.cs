using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Category-specific operations.
/// </summary>
public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<Category>> GetVisibleAsync(Guid userId, Guid? householdId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .VisibleTo(userId, householdId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyAsync(Guid userId, Guid? householdId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .VisibleTo(userId, householdId)
            .AnyAsync(cancellationToken);
    }
}
