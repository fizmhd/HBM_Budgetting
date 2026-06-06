using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Tag-specific operations.
/// </summary>
public class TagRepository : Repository<Tag>, ITagRepository
{
    public TagRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<Tag>> GetVisibleAsync(Guid userId, Guid? householdId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .VisibleTo(userId, householdId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Tag>> GetByNamesAsync(Guid userId, IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default)
    {
        if (names.Count == 0)
        {
            return new List<Tag>();
        }

        var normalised = names.Select(n => n.Trim().ToLowerInvariant()).ToList();
        return await _dbSet
            .Where(t => t.OwnerUserId == userId && normalised.Contains(t.Name))
            .ToListAsync(cancellationToken);
    }
}
