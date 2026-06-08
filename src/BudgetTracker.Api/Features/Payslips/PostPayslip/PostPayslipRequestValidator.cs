using BudgetTracker.Shared.DTOs.Payslips;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Payslips.PostPayslip;

/// <summary>
/// Validator for posting a payslip (TASK 8.4): an account and an income category are required.
/// </summary>
public class PostPayslipRequestValidator : Validator<PostPayslipRequest>
{
    public PostPayslipRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("An account is required to post a payslip.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("An income category is required to post a payslip.");
    }
}
