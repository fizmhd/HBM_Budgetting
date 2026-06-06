namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Join row linking a <see cref="Transaction"/> to a <see cref="Tag"/> (many-to-many).
/// </summary>
public class TransactionTag
{
    public Guid TransactionId { get; set; }
    public Guid TagId { get; set; }

    public Tag? Tag { get; set; }
}
