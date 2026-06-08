using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Services.Payslips;

/// <summary>
/// The Sweden country profile (TASK 8.2): Swedish summary labels over the standard reconciliation.
/// The first <see cref="ICountryPayslipProfile"/>; the arithmetic is the country-agnostic
/// <see cref="PayslipReconciler"/>, so adding another Nordic profile is just labels (and, if needed,
/// its own reconcile).
/// </summary>
public sealed class SwedenPayslipProfile : ICountryPayslipProfile
{
    public PayslipCountry Country => PayslipCountry.Sweden;

    public PayslipLabels Labels { get; } = new(
        Gross: "Bruttolön",
        Benefits: "Förmåner",
        Tax: "Skatt",
        Net: "Nettolön");

    public PayslipSummary Summarize(IReadOnlyCollection<PayslipLineItem> lines) =>
        PayslipReconciler.Summarize(lines);

    public PayslipReconciliation Reconcile(IReadOnlyCollection<PayslipLineItem> lines, decimal declaredNet) =>
        PayslipReconciler.Reconcile(lines, declaredNet);
}
