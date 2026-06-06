using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.DTOs.Transactions;

namespace BudgetTracker.Api.Features.Transactions;

/// <summary>
/// Mapping helpers between Transaction entities and DTOs.
/// </summary>
public static class TransactionMapping
{
    public static TransactionDto ToDto(this Transaction transaction,
        IReadOnlyDictionary<Guid, string> accountNames,
        IReadOnlyDictionary<Guid, string> categoryNames) => new()
    {
        Id = transaction.Id,
        AccountId = transaction.AccountId,
        AccountName = accountNames.GetValueOrDefault(transaction.AccountId),
        Date = transaction.Date,
        Type = transaction.Type.ToString(),
        Amount = transaction.Amount,
        CurrencyCode = transaction.CurrencyCode,
        Description = transaction.Description,
        Notes = transaction.Notes,
        CounterAccountId = transaction.CounterAccountId,
        CounterAccountName = transaction.CounterAccountId is { } id ? accountNames.GetValueOrDefault(id) : null,
        IsShared = transaction.Visibility == Visibility.HouseholdShared,
        Splits = transaction.Splits
            .Select(s => new TransactionSplitDto
            {
                Id = s.Id,
                CategoryId = s.CategoryId,
                CategoryName = categoryNames.GetValueOrDefault(s.CategoryId),
                Amount = s.Amount,
                Note = s.Note
            })
            .ToList(),
        Tags = transaction.TransactionTags
            .Where(tt => tt.Tag is not null)
            .Select(tt => tt.Tag!.Name)
            .OrderBy(n => n)
            .ToList()
    };
}
