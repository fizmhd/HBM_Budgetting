namespace BudgetTracker.Shared.DTOs.Accounts;

/// <summary>
/// An account/card visible to the caller.
/// </summary>
public class AccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Account type ("Bank", "Cash", "CreditCard", "Savings").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "SEK";
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Balance shown to the user. Equals <see cref="OpeningBalance"/> until transactions land
    /// (Sprint 4), after which it becomes the derived live balance.
    /// </summary>
    public decimal Balance { get; set; }

    public decimal? CreditLimit { get; set; }
    public bool IsArchived { get; set; }

    /// <summary>
    /// Visibility scope ("Individual" or "HouseholdShared").
    /// </summary>
    public string Visibility { get; set; } = "Individual";

    /// <summary>
    /// True when the account is shared with the household.
    /// </summary>
    public bool IsShared { get; set; }
}

/// <summary>
/// Request to create an account.
/// </summary>
public class CreateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Bank";
    public string CurrencyCode { get; set; } = "SEK";
    public decimal OpeningBalance { get; set; }
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Share the account with the household instead of keeping it individual.
    /// </summary>
    public bool IsShared { get; set; }
}

/// <summary>
/// Request to update an account's editable fields.
/// </summary>
public class UpdateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Bank";
    public decimal? CreditLimit { get; set; }

    /// <summary>
    /// Share the account with the household instead of keeping it individual.
    /// </summary>
    public bool IsShared { get; set; }
}
