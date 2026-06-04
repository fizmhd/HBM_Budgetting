using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Auth.ForgotPassword;

/// <summary>
/// Validator for forgot password requests
/// </summary>
public class ForgotPasswordRequestValidator : Validator<Shared.DTOs.Auth.ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");
    }
}
