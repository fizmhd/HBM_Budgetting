using BudgetTracker.Shared.DTOs.Budgets;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Budgets.UpdateBudget;

/// <summary>
/// Validator for updating a budget — same rules as create.
/// </summary>
public class UpdateBudgetRequestValidator : Validator<UpdateBudgetRequest>
{
    public UpdateBudgetRequestValidator()
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
