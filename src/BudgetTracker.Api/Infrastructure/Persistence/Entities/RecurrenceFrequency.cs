namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// How often a recurring rule repeats. Combined with the rule's interval (e.g. interval 2 + Weekly =
/// every two weeks).
/// </summary>
public enum RecurrenceFrequency
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Yearly = 3
}
