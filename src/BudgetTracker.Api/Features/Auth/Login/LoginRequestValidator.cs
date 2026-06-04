using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Auth.Login;

/// <summary>
/// Validator for login requests
/// </summary>
public class LoginRequestValidator : Validator<Shared.DTOs.Auth.LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}
