namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// A place money lives — a bank account, cash, credit card, or savings account. Owned by a user and
/// optionally shared with their household (via <see cref="OwnedEntity.Visibility"/>).
/// </summary>
public class Account : OwnedEntity
{
    /// <summary>
    /// Display name of the account.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kind of account.
    /// </summary>
    public AccountType Type { get; set; } = AccountType.Bank;

    /// <summary>
    /// ISO currency code. Always "SEK" in the MVP, but stored per-account so the schema is
    /// currency-ready.
    /// </summary>
    public string CurrencyCode { get; set; } = "SEK";

    /// <summary>
    /// Balance at the point the account was added. The live balance is derived as
    /// <c>OpeningBalance + Σ(transaction effects)</c> from Sprint 4 onward; until then the opening
    /// balance is shown.
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Credit limit. Only meaningful for <see cref="AccountType.CreditCard"/>; null otherwise.
    /// </summary>
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Whether the account has been archived (hidden from active use).
    /// </summary>
    public bool IsArchived { get; set; }
}
