namespace BudgetTracker.Shared.DTOs.Transactions;

/// <summary>
/// A category allocation within a transaction.
/// </summary>
public class TransactionSplitDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }

    /// <summary>Resolved category name, when available.</summary>
    public string? CategoryName { get; set; }

    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A transaction visible to the caller, with its splits and tags.
/// </summary>
public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string? AccountName { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>"Income", "Expense", or "Transfer".</summary>
    public string Type { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "SEK";
    public string? Description { get; set; }
    public string? Notes { get; set; }

    public Guid? CounterAccountId { get; set; }
    public string? CounterAccountName { get; set; }

    public List<TransactionSplitDto> Splits { get; set; } = new();
    public List<string> Tags { get; set; } = new();

    public bool IsShared { get; set; }
}

/// <summary>
/// A page of transactions with paging metadata.
/// </summary>
public class TransactionListResponse
{
    public List<TransactionDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

/// <summary>
/// A split supplied when creating/updating a transaction.
/// </summary>
public class TransactionSplitInput
{
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Request to create a transaction (income/expense with splits, or a transfer).
/// </summary>
public class CreateTransactionRequest
{
    /// <summary>"Income", "Expense", or "Transfer".</summary>
    public string Type { get; set; } = "Expense";

    public Guid AccountId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "SEK";
    public string? Description { get; set; }
    public string? Notes { get; set; }

    /// <summary>Destination account for a transfer.</summary>
    public Guid? CounterAccountId { get; set; }

    /// <summary>Category splits for income/expense. Omitted/empty for transfers.</summary>
    public List<TransactionSplitInput> Splits { get; set; } = new();

    /// <summary>Free-form tag names to attach (created on demand).</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Share with the household instead of keeping individual.</summary>
    public bool IsShared { get; set; }
}

/// <summary>
/// Request to update a transaction. Same shape as create.
/// </summary>
public class UpdateTransactionRequest : CreateTransactionRequest
{
}
