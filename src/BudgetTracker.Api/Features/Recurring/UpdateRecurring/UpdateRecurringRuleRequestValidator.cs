using BudgetTracker.Shared.DTOs.Recurring;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Recurring.UpdateRecurring;

/// <summary>
/// Validator for updating a recurring rule — same rules as create.
/// </summary>
public class UpdateRecurringRuleRequestValidator : Validator<UpdateRecurringRuleRequest>
{
    public UpdateRecurringRuleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Interval)
            .GreaterThanOrEqualTo(1).WithMessage("Interval must be at least 1.");

        RuleFor(x => x.DayOfMonth)
            .InclusiveBetween(1, 31).When(x => x.DayOfMonth.HasValue)
            .WithMessage("Day of month must be between 1 and 31.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue)
            .WithMessage("End date must be on or after the start date.");

        RuleFor(x => x.CategoryId)
            .NotNull().WithMessage("A category is required.");
    }
}
