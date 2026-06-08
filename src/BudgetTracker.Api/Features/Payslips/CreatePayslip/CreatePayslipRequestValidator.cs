using BudgetTracker.Shared.DTOs.Payslips;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Payslips.CreatePayslip;

/// <summary>
/// Validator for creating/updating a payslip (TASK 8.3): employer required, valid pay period, and
/// non-negative amounts on every line (the sign is implied by the line type, so amounts are positive).
/// </summary>
public class CreatePayslipRequestValidator : Validator<CreatePayslipRequest>
{
    public CreatePayslipRequestValidator()
    {
        RuleFor(x => x.EmployerName)
            .NotEmpty().WithMessage("Employer is required.")
            .MaximumLength(200).WithMessage("Employer cannot exceed 200 characters.");

        RuleFor(x => x.EmployeeName)
            .MaximumLength(200).WithMessage("Employee name cannot exceed 200 characters.");

        RuleFor(x => x.PayPeriodEnd)
            .GreaterThanOrEqualTo(x => x.PayPeriodStart)
            .WithMessage("Pay period end must be on or after the start.");

        RuleFor(x => x.DeclaredNet)
            .GreaterThanOrEqualTo(0).WithMessage("Net pay cannot be negative.");

        RuleForEach(x => x.LineItems).ChildRules(line =>
        {
            line.RuleFor(l => l.Label)
                .NotEmpty().WithMessage("Each line needs a label.")
                .MaximumLength(200).WithMessage("Line label cannot exceed 200 characters.");
            line.RuleFor(l => l.Amount)
                .GreaterThanOrEqualTo(0).WithMessage("Line amount cannot be negative.");
        });

        RuleForEach(x => x.LeaveBalances).ChildRules(balance =>
        {
            balance.RuleFor(b => b.LeaveType)
                .MaximumLength(100).WithMessage("Leave type cannot exceed 100 characters.");
        });
    }
}
