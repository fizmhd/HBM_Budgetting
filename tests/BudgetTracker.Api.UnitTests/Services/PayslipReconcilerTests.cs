using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services.Payslips;
using FluentAssertions;

namespace BudgetTracker.Api.UnitTests.Services;

/// <summary>
/// Unit tests for the payslip summary/reconciliation (TASK 8.2): gross/benefit/tax totals from typed
/// line items and the net comparison against the printed net. Anchored on the spec's worked example
/// (net 41 557) where a taxable benefit raises the tax base but is not paid out in cash.
/// </summary>
public class PayslipReconcilerTests
{
    private static PayslipLineItem Line(PayslipLineType type, decimal amount) =>
        new() { Type = type, Amount = amount, Label = type.ToString() };

    [Fact]
    public void Reconciles_a_real_lonespecifikation_to_net()
    {
        // Grundlön 55 000, Bilförmån 2 000 (taxable, not cash), Preliminärskatt 13 443.
        // Net = 55 000 - 13 443 = 41 557; the benefit only raises the tax base.
        var lines = new[]
        {
            Line(PayslipLineType.Earning, 55_000m),
            Line(PayslipLineType.Benefit, 2_000m),
            Line(PayslipLineType.Tax, 13_443m)
        };

        var result = PayslipReconciler.Reconcile(lines, declaredNet: 41_557m);

        result.Summary.Gross.Should().Be(55_000m);
        result.Summary.Benefits.Should().Be(2_000m);
        result.Summary.Tax.Should().Be(13_443m);
        result.Summary.Net.Should().Be(41_557m);
        result.Difference.Should().Be(0m);
        result.IsReconciled.Should().BeTrue();
    }

    [Fact]
    public void Deductions_and_reimbursements_move_net()
    {
        var lines = new[]
        {
            Line(PayslipLineType.Earning, 30_000m),
            Line(PayslipLineType.Tax, 9_000m),
            Line(PayslipLineType.Deduction, 200m),       // e.g. union fee
            Line(PayslipLineType.Reimbursement, 500m)    // e.g. expense payout (non-taxable)
        };

        var summary = PayslipReconciler.Summarize(lines);

        // 30 000 + 500 - 9 000 - 200
        summary.Net.Should().Be(21_300m);
        summary.Deductions.Should().Be(200m);
        summary.Reimbursements.Should().Be(500m);
    }

    [Fact]
    public void Info_lines_do_not_affect_any_total()
    {
        var lines = new[]
        {
            Line(PayslipLineType.Earning, 1_000m),
            Line(PayslipLineType.Info, 99_999m)
        };

        var summary = PayslipReconciler.Summarize(lines);

        summary.Gross.Should().Be(1_000m);
        summary.Net.Should().Be(1_000m);
    }

    [Fact]
    public void Off_by_more_than_tolerance_does_not_reconcile()
    {
        var lines = new[] { Line(PayslipLineType.Earning, 1_000m) };

        var result = PayslipReconciler.Reconcile(lines, declaredNet: 900m);

        result.Difference.Should().Be(100m);
        result.IsReconciled.Should().BeFalse();
    }

    [Fact]
    public void Within_a_cent_still_reconciles()
    {
        var lines = new[] { Line(PayslipLineType.Earning, 1_000m) };

        PayslipReconciler.Reconcile(lines, declaredNet: 1_000.01m).IsReconciled.Should().BeTrue();
        PayslipReconciler.Reconcile(lines, declaredNet: 999.99m).IsReconciled.Should().BeTrue();
    }
}
