using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Filter/sort/page criteria for listing transactions.
/// </summary>
public sealed class TransactionListFilter
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public Guid? AccountId { get; init; }

    /// <summary>When true, return only account-less ("cash") entries. Ignored if <see cref="AccountId"/> is set.</summary>
    public bool NoAccount { get; init; }

    public Guid? CategoryId { get; init; }
    public TransactionType? Type { get; init; }
    public string? Tag { get; init; }
    public string? Search { get; init; }

    /// <summary>"date" (default) or "amount".</summary>
    public string Sort { get; init; } = "date";

    /// <summary>Descending when true (default for date — newest first).</summary>
    public bool Descending { get; init; } = true;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

/// <summary>
/// A page of transactions plus the total matching count.
/// </summary>
public sealed record TransactionPage(List<Transaction> Items, int TotalCount);

/// <summary>
/// Repository for Transaction-specific operations.
/// </summary>
public interface ITransactionRepository : IRepository<Transaction>
{
    /// <summary>
    /// Lists the transactions visible to the caller matching the filter, sorted and paged. Items
    /// include their splits and tag joins.
    /// </summary>
    Task<TransactionPage> ListAsync(Guid userId, Guid? householdId, TransactionListFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single transaction (with splits and tags) by id, without applying visibility — the
    /// caller is expected to enforce <see cref="OwnedEntityQueryExtensions.IsVisibleTo"/>.
    /// </summary>
    Task<Transaction?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if any visible transaction split references the given category. Backs the category
    /// deletion rule (TASK 3.2).
    /// </summary>
    Task<bool> AnySplitForCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sums the signed balance effect of every transaction visible to the caller, grouped by account
    /// (income +, expense −, transfer − on source / + on destination). Backs balance computation.
    /// </summary>
    Task<Dictionary<Guid, decimal>> GetBalanceDeltasAsync(Guid userId, Guid? householdId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the given splits for deletion (used when replacing a transaction's splits on update).
    /// </summary>
    void RemoveSplits(IEnumerable<TransactionSplit> splits);

    /// <summary>
    /// Marks the given tag joins for deletion (used when replacing a transaction's tags on update).
    /// </summary>
    void RemoveTags(IEnumerable<TransactionTag> tags);

    /// <summary>
    /// Marks the given tag joins as added. Needed because a join row's composite key is always set, so
    /// EF would otherwise treat a navigation-discovered join as an existing (Modified) row.
    /// </summary>
    void AddTags(IEnumerable<TransactionTag> tags);
}
