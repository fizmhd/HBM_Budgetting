namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// The kind of a single payslip line, which fixes its sign and how it feeds the summary and the
/// net-pay reconciliation. Amounts are always stored positive; the type implies the direction.
/// This typed-line vocabulary is country-agnostic — a Swedish <c>Grundlön</c> is an
/// <see cref="Earning"/>, a <c>Bilförmån</c> a <see cref="Benefit"/>, <c>Preliminärskatt</c> a
/// <see cref="Tax"/> — so other country profiles reuse the same shape.
/// </summary>
public enum PayslipLineType
{
    /// <summary>Taxable cash earning (e.g. base salary, overtime). Adds to gross and to net.</summary>
    Earning = 1,

    /// <summary>Taxable non-cash benefit / förmån (e.g. car benefit). Adds to the tax base but not to net cash.</summary>
    Benefit = 2,

    /// <summary>Tax withheld (e.g. preliminärskatt). Reduces net.</summary>
    Tax = 3,

    /// <summary>Other deduction (e.g. union fee, benefit offset). Reduces net.</summary>
    Deduction = 4,

    /// <summary>Non-taxable cash reimbursement (e.g. expense payout). Adds to net but not to gross/tax base.</summary>
    Reimbursement = 5,

    /// <summary>Informational line only (e.g. accrued pension). Affects neither the summary nor net.</summary>
    Info = 6
}
