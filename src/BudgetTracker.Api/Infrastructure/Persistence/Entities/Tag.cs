namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// A reusable free-form label a user can attach to transactions. Scoped per owner/household and
/// unique by name within that scope.
/// </summary>
public class Tag : OwnedEntity
{
    /// <summary>
    /// Tag text (e.g. "vacation-2026", "reimbursable").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Transaction join rows referencing this tag.
    /// </summary>
    public List<TransactionTag> TransactionTags { get; set; } = new();
}
