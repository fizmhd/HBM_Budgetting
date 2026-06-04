using BudgetTracker.Api.Infrastructure.Security;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Auth.ResetPassword;

/// <summary>
/// Validator for reset password requests
/// </summary>
public class ResetPasswordRequestValidator : Validator<Shared.DTOs.Auth.ResetPasswordRequest>
{
    public ResetPasswordRequestValidator(PasswordValidator passwordValidator)
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Reset token is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage("New password is required")
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
    }
}
