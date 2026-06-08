using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Services.Payslips;

/// <summary>
/// A country profile (TASK 8.2): the seam that lets the generic payslip shape be read against a
/// specific country's conventions — localized summary labels and the gross/benefit/tax/net
/// reconciliation. Sweden is the first profile; other Nordic profiles can be added later (D10) by
/// registering another implementation, with no change to the model or endpoints.
/// </summary>
public interface ICountryPayslipProfile
{
    /// <summary>The country this profile handles.</summary>
    PayslipCountry Country { get; }

    /// <summary>Localized labels for the summary totals (e.g. "Bruttolön", "Förmåner", "Skatt").</summary>
    PayslipLabels Labels { get; }

    /// <summary>Sums the line items into the month summary.</summary>
    PayslipSummary Summarize(IReadOnlyCollection<PayslipLineItem> lines);

    /// <summary>Summarizes and reconciles the line items against the printed net.</summary>
    PayslipReconciliation Reconcile(IReadOnlyCollection<PayslipLineItem> lines, decimal declaredNet);
}

/// <summary>Localized labels for the four payslip summary totals.</summary>
/// <param name="Gross">Label for gross pay (Swedish: "Bruttolön").</param>
/// <param name="Benefits">Label for taxable benefits (Swedish: "Förmåner").</param>
/// <param name="Tax">Label for tax withheld (Swedish: "Skatt").</param>
/// <param name="Net">Label for net pay (Swedish: "Nettolön").</param>
public sealed record PayslipLabels(string Gross, string Benefits, string Tax, string Net);
