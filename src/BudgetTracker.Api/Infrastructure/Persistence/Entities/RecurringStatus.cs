namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Lifecycle state of a recurring rule. A Paused rule is skipped by the generation engine until resumed.
/// </summary>
public enum RecurringStatus
{
    Active = 0,
    Paused = 1
}
