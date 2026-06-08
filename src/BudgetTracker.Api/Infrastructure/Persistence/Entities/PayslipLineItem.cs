namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// A single line on a payslip (TASK 8.1), owned by its <see cref="Payslip"/>. Its <see cref="Type"/>
/// fixes the sign and how it feeds the summary/reconciliation; <see cref="Amount"/> is always positive.
/// </summary>
public class PayslipLineItem : BaseEntity
{
    public Guid PayslipId { get; set; }

    public PayslipLineType Type { get; set; }

    /// <summary>Label as printed (e.g. "Grundlön", "Bilförmån", "Preliminärskatt").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Optional quantity (e.g. hours, days) for lines billed per unit.</summary>
    public decimal? Quantity { get; set; }

    /// <summary>Optional per-unit amount paired with <see cref="Quantity"/>.</summary>
    public decimal? UnitAmount { get; set; }

    /// <summary>Line total, always positive. Direction is implied by <see cref="Type"/>.</summary>
    public decimal Amount { get; set; }

    /// <summary>Display order within the payslip.</summary>
    public int SortOrder { get; set; }
}
