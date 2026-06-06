using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for RecurringOccurrence-specific operations.
/// </summary>
public class RecurringOccurrenceRepository : Repository<RecurringOccurrence>, IRecurringOccurrenceRepository
{
    public RecurringOccurrenceRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid recurringRuleId, DateOnly dueDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(o => o.RecurringRuleId == recurringRuleId && o.DueDate == dueDate,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<RecurringOccurrence>> GetByRuleAsync(Guid recurringRuleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.RecurringRuleId == recurringRuleId)
            .OrderByDescending(o => o.DueDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<(RecurringOccurrence Occurrence, RecurringRule Rule)>> GetPendingVisibleAsync(
        Guid userId, Guid? householdId, CancellationToken cancellationToken = default)
    {
        var rules = _context.Set<RecurringRule>().VisibleTo(userId, householdId);

        var rows = await (
            from o in _dbSet
            join r in rules on o.RecurringRuleId equals r.Id
            where o.Status == OccurrenceStatus.Pending
            orderby o.DueDate
            select new { Occurrence = o, Rule = r })
            .ToListAsync(cancellationToken);

        return rows.Select(x => (x.Occurrence, x.Rule)).ToList();
    }
}
