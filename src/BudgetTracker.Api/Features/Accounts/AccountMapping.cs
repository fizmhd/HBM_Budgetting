using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.DTOs.Accounts;

namespace BudgetTracker.Api.Features.Accounts;

/// <summary>
/// Mapping helpers between Account entities and DTOs.
/// </summary>
public static class AccountMapping
{
    /// <summary>
    /// Currencies accepted in the MVP. SEK is the base; USD/INR cover the user's foreign inflows.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedCurrencies =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SEK", "USD", "EUR", "GBP", "NOK", "DKK", "INR" };

    /// <summary>
    /// Maps an account to its DTO. <paramref name="currentBalance"/> is the derived live balance
    /// (TASK 4.4); pass null to fall back to the opening balance (e.g. right after creation).
    /// </summary>
    public static AccountDto ToDto(this Account account, decimal? currentBalance = null) => new()
    {
        Id = account.Id,
        Name = account.Name,
        Type = account.Type.ToString(),
        CurrencyCode = account.CurrencyCode,
        OpeningBalance = account.OpeningBalance,
        Balance = currentBalance ?? account.OpeningBalance,
        CreditLimit = account.CreditLimit,
        IsArchived = account.IsArchived,
        Visibility = account.Visibility.ToString(),
        IsShared = account.Visibility == Visibility.HouseholdShared
    };

    public static bool TryParseType(string? value, out AccountType type) =>
        Enum.TryParse(value, ignoreCase: true, out type) && Enum.IsDefined(type);
}
