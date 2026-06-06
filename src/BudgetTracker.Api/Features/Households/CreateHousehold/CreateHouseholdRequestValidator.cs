using BudgetTracker.Shared.DTOs.Households;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Households.CreateHousehold;

/// <summary>
/// Validator for creating a household.
/// </summary>
public class CreateHouseholdRequestValidator : Validator<CreateHouseholdRequest>
{
    public CreateHouseholdRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Household name is required")
            .MaximumLength(100)
            .WithMessage("Household name cannot exceed 100 characters");
    }
}
