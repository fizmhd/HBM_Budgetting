using BudgetTracker.Api.Features.Accounts;
using BudgetTracker.Shared.DTOs.Accounts;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Accounts.UpdateAccount;

/// <summary>
/// Validator for updating an account.
/// </summary>
public class UpdateAccountRequestValidator : Validator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Account name is required")
            .MaximumLength(100).WithMessage("Account name cannot exceed 100 characters");

        RuleFor(x => x.Type)
            .Must(t => AccountMapping.TryParseType(t, out _))
            .WithMessage("Account type must be one of Bank, Cash, CreditCard, Savings");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).When(x => x.CreditLimit.HasValue)
            .WithMessage("Credit limit cannot be negative");

        RuleFor(x => x.CreditLimit)
            .Null()
            .When(x => !string.Equals(x.Type, "CreditCard", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Credit limit is only allowed on credit cards");
    }
}
