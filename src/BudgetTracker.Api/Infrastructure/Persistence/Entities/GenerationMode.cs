namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// How a recurring rule materialises a due occurrence (R6, D5).
/// </summary>
public enum GenerationMode
{
    /// <summary>Create the transaction immediately and mark the occurrence Posted.</summary>
    AutoPost = 0,

    /// <summary>Create a Pending occurrence with no transaction until the user confirms.</summary>
    PendingConfirm = 1
}
