using FastEndpoints;
using FluentValidation;
using BudgetTracker.Shared.DTOs.Users;

namespace BudgetTracker.Api.Features.Users.CompleteProfile;

/// <summary>
/// Validator for complete profile requests
/// </summary>
public class CompleteProfileRequestValidator : Validator<CompleteProfileRequest>
{
    public CompleteProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(100)
            .WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(100)
            .WithMessage("Last name cannot exceed 100 characters");
    }
}
