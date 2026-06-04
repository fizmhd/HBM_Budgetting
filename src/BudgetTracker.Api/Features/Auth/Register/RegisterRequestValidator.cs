using BudgetTracker.Api.Infrastructure.Security;
using BudgetTracker.Shared.DTOs.Auth;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Auth.Register;

/// <summary>
/// Validator for RegisterRequest
/// </summary>
public class RegisterRequestValidator : Validator<RegisterRequest>
{
    public RegisterRequestValidator(PasswordValidator passwordValidator)
    {
        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(255)
            .WithMessage("Email cannot exceed 255 characters");

        // Password validation
        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MaximumLength(100)
            .WithMessage("Password cannot exceed 100 characters")
            .Custom((password, context) =>
            {
                var errors = passwordValidator.Validate(password);
                foreach (var error in errors)
                {
                    context.AddFailure(error);
                }
            });

        // Confirm password validation
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required")
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match");
    }
}
