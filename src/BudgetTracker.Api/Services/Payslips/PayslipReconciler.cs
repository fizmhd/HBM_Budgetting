using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Services.Payslips;

/// <summary>
/// The country-agnostic core of the payslip summary/reconciliation (TASK 8.2). Amounts on a line are
/// stored positive; the <see cref="PayslipLineType"/> fixes the sign and which total it feeds:
/// <list type="bullet">
/// <item><b>Gross</b> (Bruttolön) = Σ Earning — taxable cash earnings.</item>
/// <item><b>Benefits</b> (Förmåner) = Σ Benefit — taxable non-cash benefits; raise the tax base, not net.</item>
/// <item><b>Tax</b> (Skatt) = Σ Tax — withheld; reduces net.</item>
/// <item><b>Net</b> = Earnings + Reimbursements − Tax − Deductions — the cash that actually lands.</item>
/// </list>
/// Country profiles (<see cref="ICountryPayslipProfile"/>) reuse this and add localized labels; a future
/// profile with different sign conventions can override the arithmetic.
/// </summary>
public static class PayslipReconciler
{
    /// <summary>The computed net is treated as matching the printed net within this absolute tolerance.</summary>
    public const decimal Tolerance = 0.01m;

    /// <summary>Sums the typed line items into the month summary (gross/benefits/tax/deductions/net).</summary>
    public static PayslipSummary Summarize(IEnumerable<PayslipLineItem> lines)
    {
        decimal gross = 0m, benefits = 0m, tax = 0m, deductions = 0m, reimbursements = 0m;

        foreach (var line in lines)
        {
            switch (line.Type)
            {
                case PayslipLineType.Earning:
                    gross += line.Amount;
                    break;
                case PayslipLineType.Benefit:
                    benefits += line.Amount;
                    break;
                case PayslipLineType.Tax:
                    tax += line.Amount;
                    break;
                case PayslipLineType.Deduction:
                    deductions += line.Amount;
                    break;
                case PayslipLineType.Reimbursement:
                    reimbursements += line.Amount;
                    break;
                case PayslipLineType.Info:
                    // Informational only — contributes to no total.
                    break;
            }
        }

        var net = gross + reimbursements - tax - deductions;
        return new PayslipSummary(gross, benefits, tax, deductions, reimbursements, net);
    }

    /// <summary>
    /// Summarizes the lines and compares the computed net against the net printed on the payslip.
    /// </summary>
    public static PayslipReconciliation Reconcile(IEnumerable<PayslipLineItem> lines, decimal declaredNet)
    {
        var summary = Summarize(lines);
        var difference = summary.Net - declaredNet;
        var isReconciled = Math.Abs(difference) <= Tolerance;
        return new PayslipReconciliation(summary, declaredNet, difference, isReconciled);
    }
}

/// <summary>The totals derived from a payslip's line items for one pay period.</summary>
/// <param name="Gross">Taxable cash earnings (Bruttolön).</param>
/// <param name="Benefits">Taxable non-cash benefits (Förmåner).</param>
/// <param name="Tax">Tax withheld (Skatt).</param>
/// <param name="Deductions">Other deductions reducing net.</param>
/// <param name="Reimbursements">Non-taxable cash reimbursements added to net.</param>
/// <param name="Net">Computed net pay = Gross + Reimbursements − Tax − Deductions.</param>
public sealed record PayslipSummary(
    decimal Gross,
    decimal Benefits,
    decimal Tax,
    decimal Deductions,
    decimal Reimbursements,
    decimal Net);

/// <summary>The month summary plus the comparison against the net printed on the payslip.</summary>
/// <param name="Summary">The computed totals.</param>
/// <param name="DeclaredNet">The net printed on the payslip.</param>
/// <param name="Difference">Computed net minus declared net (0 when they agree).</param>
/// <param name="IsReconciled">True when the computed and declared nets agree within tolerance.</param>
public sealed record PayslipReconciliation(
    PayslipSummary Summary,
    decimal DeclaredNet,
    decimal Difference,
    bool IsReconciled);
