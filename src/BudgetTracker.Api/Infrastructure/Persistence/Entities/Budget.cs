namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// A spending limit for one category over a period (TASK 6.1). Owned by a user and optionally shared
/// with their household (via <see cref="OwnedEntity.Visibility"/>). "Spent", "remaining", percent-used
/// and status are all derived at read time from transaction splits (TASK 6.2) — only the limit itself
/// and the alert bookkeeping are persisted.
/// </summary>
public class Budget : OwnedEntity
{
    /// <summary>
    /// Category this budget caps. Exact category only in the MVP — no descendant roll-up (TASK 6.2).
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Whether the period is a calendar month or an arbitrary custom range.
    /// </summary>
    public BudgetPeriodType PeriodType { get; set; }

    /// <summary>
    /// First day of the budget period (inclusive).
    /// </summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>
    /// Last day of the budget period (inclusive).
    /// </summary>
    public DateOnly PeriodEnd { get; set; }

    /// <summary>
    /// The spending limit for the period, always positive.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Percent-used at which a warning/alert is raised. Defaults to 80.
    /// </summary>
    public int AlertThresholdPercent { get; set; } = 80;

    /// <summary>
    /// The highest alert level (0 = none, <see cref="AlertThresholdPercent"/>, or 100) for which an
    /// email has already been sent in the current run of spending. Used to send a single email per
    /// threshold crossing (TASK 6.3); reset to 0 when usage falls back to OK so a later re-crossing
    /// alerts again.
    /// </summary>
    public int LastAlertedThreshold { get; set; }
}
