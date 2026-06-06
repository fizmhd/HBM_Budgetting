using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Transaction-specific operations.
/// </summary>
public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<TransactionPage> ListAsync(Guid userId, Guid? householdId, TransactionListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.VisibleTo(userId, householdId);

        if (filter.From is { } from)
        {
            query = query.Where(t => t.Date >= from);
        }
        if (filter.To is { } to)
        {
            query = query.Where(t => t.Date <= to);
        }
        if (filter.AccountId is { } accountId)
        {
            // Match either side of a transfer as well as plain income/expense.
            query = query.Where(t => t.AccountId == accountId || t.CounterAccountId == accountId);
        }
        if (filter.Type is { } type)
        {
            query = query.Where(t => t.Type == type);
        }
        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(t => t.Splits.Any(s => s.CategoryId == categoryId));
        }
        if (!string.IsNullOrWhiteSpace(filter.Tag))
        {
            var tag = filter.Tag.Trim().ToLowerInvariant();
            query = query.Where(t => t.TransactionTags.Any(tt => tt.Tag!.Name == tag));
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(t =>
                (t.Description != null && EF.Functions.ILike(t.Description, $"%{term}%")) ||
                (t.Notes != null && EF.Functions.ILike(t.Notes, $"%{term}%")));
        }

        var total = await query.CountAsync(cancellationToken);

        query = (filter.Sort?.ToLowerInvariant(), filter.Descending) switch
        {
            ("amount", true) => query.OrderByDescending(t => t.Amount).ThenByDescending(t => t.Date),
            ("amount", false) => query.OrderBy(t => t.Amount).ThenBy(t => t.Date),
            (_, false) => query.OrderBy(t => t.Date).ThenBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt)
        };

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.Splits)
            .Include(t => t.TransactionTags)
                .ThenInclude(tt => tt.Tag)
            .ToListAsync(cancellationToken);

        return new TransactionPage(items, total);
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Splits)
            .Include(t => t.TransactionTags)
                .ThenInclude(tt => tt.Tag)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AnySplitForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TransactionSplit>()
            .AnyAsync(s => s.CategoryId == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, decimal>> GetBalanceDeltasAsync(Guid userId, Guid? householdId,
        CancellationToken cancellationToken = default)
    {
        var visible = await _dbSet
            .VisibleTo(userId, householdId)
            .Select(t => new { t.Type, t.Amount, t.AccountId, t.CounterAccountId })
            .ToListAsync(cancellationToken);

        var deltas = new Dictionary<Guid, decimal>();

        void Apply(Guid accountId, decimal delta)
        {
            deltas[accountId] = deltas.TryGetValue(accountId, out var current) ? current + delta : delta;
        }

        foreach (var t in visible)
        {
            switch (t.Type)
            {
                case TransactionType.Income:
                    Apply(t.AccountId, t.Amount);
                    break;
                case TransactionType.Expense:
                    Apply(t.AccountId, -t.Amount);
                    break;
                case TransactionType.Transfer:
                    Apply(t.AccountId, -t.Amount);
                    if (t.CounterAccountId is { } dest)
                    {
                        Apply(dest, t.Amount);
                    }
                    break;
            }
        }

        return deltas;
    }

    /// <inheritdoc />
    public void RemoveSplits(IEnumerable<TransactionSplit> splits)
    {
        _context.Set<TransactionSplit>().RemoveRange(splits);
    }

    /// <inheritdoc />
    public void RemoveTags(IEnumerable<TransactionTag> tags)
    {
        _context.Set<TransactionTag>().RemoveRange(tags);
    }

    /// <inheritdoc />
    public void AddTags(IEnumerable<TransactionTag> tags)
    {
        _context.Set<TransactionTag>().AddRange(tags);
    }
}
