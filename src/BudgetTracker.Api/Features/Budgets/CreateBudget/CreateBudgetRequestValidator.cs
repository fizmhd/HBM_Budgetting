using BudgetTracker.Shared.DTOs.Budgets;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Budgets.CreateBudget;

/// <summary>
/// Validator for creating a budget (TASK 6.4): amount &gt; 0; valid period (start ≤ end); sane threshold.
/// </summary>
public class CreateBudgetRequestValidator : Validator<CreateBudgetRequest>
{
    public CreateBudgetRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("A category is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Budget amount must be greater than zero.");

        RuleFor(x => x.AlertThresholdPercent)
            .InclusiveBetween(1, 100).WithMessage("Alert threshold must be between 1 and 100 percent.");

        RuleFor(x => x.PeriodEnd)
            .GreaterThanOrEqualTo(x => x.PeriodStart)
            .WithMessage("Period end must be on or after period start.");
    }
}
