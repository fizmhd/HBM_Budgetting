namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Kind of account money can live in.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// A regular bank/checking account.
    /// </summary>
    Bank = 0,

    /// <summary>
    /// Physical cash.
    /// </summary>
    Cash = 1,

    /// <summary>
    /// A credit card (the only type that may carry a credit limit).
    /// </summary>
    CreditCard = 2,

    /// <summary>
    /// A savings account.
    /// </summary>
    Savings = 3
}
