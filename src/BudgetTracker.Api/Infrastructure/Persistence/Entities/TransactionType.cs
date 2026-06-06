namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// The kind of money movement a transaction represents.
/// </summary>
public enum TransactionType
{
    /// <summary>Money coming in (increases the account balance). Carries category splits.</summary>
    Income = 0,

    /// <summary>Money going out (decreases the account balance). Carries category splits.</summary>
    Expense = 1,

    /// <summary>
    /// A move between two of the caller's accounts. Neither income nor expense; carries no category
    /// splits. <see cref="Transaction.CounterAccountId"/> is the destination.
    /// </summary>
    Transfer = 2
}
