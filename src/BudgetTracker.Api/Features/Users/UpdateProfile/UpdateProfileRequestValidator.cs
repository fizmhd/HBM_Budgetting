using FastEndpoints;
using FluentValidation;
using BudgetTracker.Shared.DTOs.Users;

namespace BudgetTracker.Api.Features.Users.UpdateProfile;

/// <summary>
/// Validator for update profile requests
/// </summary>
public class UpdateProfileRequestValidator : Validator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.FirstName), () =>
        {
            RuleFor(x => x.FirstName)
                .MaximumLength(100)
                .WithMessage("First name cannot exceed 100 characters");
        });

        When(x => !string.IsNullOrEmpty(x.LastName), () =>
        {
            RuleFor(x => x.LastName)
                .MaximumLength(100)
                .WithMessage("Last name cannot exceed 100 characters");
        });
    }
}
