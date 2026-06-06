using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for RecurringRule-specific operations.
/// </summary>
public class RecurringRuleRepository : Repository<RecurringRule>, IRecurringRuleRepository
{
    public RecurringRuleRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<RecurringRule>> GetVisibleAsync(Guid userId, Guid? householdId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .VisibleTo(userId, householdId)
            .OrderBy(r => r.NextDueDate)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<RecurringRule>> GetDueAsync(DateOnly asOf, Guid? ownerFilter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(r =>
            r.Status == RecurringStatus.Active &&
            r.NextDueDate <= asOf &&
            (r.EndDate == null || r.NextDueDate <= r.EndDate));

        if (ownerFilter is { } owner)
        {
            query = query.Where(r => r.OwnerUserId == owner);
        }

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnyForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(r => r.CategoryId == categoryId, cancellationToken);
    }
}
