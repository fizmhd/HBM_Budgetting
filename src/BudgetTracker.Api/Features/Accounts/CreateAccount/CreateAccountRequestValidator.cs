using BudgetTracker.Api.Features.Accounts;
using BudgetTracker.Shared.DTOs.Accounts;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Accounts.CreateAccount;

/// <summary>
/// Validator for creating an account.
/// </summary>
public class CreateAccountRequestValidator : Validator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Account name is required")
            .MaximumLength(100).WithMessage("Account name cannot exceed 100 characters");

        RuleFor(x => x.Type)
            .Must(t => AccountMapping.TryParseType(t, out _))
            .WithMessage("Account type must be one of Bank, Cash, CreditCard, Savings");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency is required")
            .Must(c => AccountMapping.AllowedCurrencies.Contains(c))
            .WithMessage("Currency is not supported");

        // Credit limit only valid for credit cards, and never negative.
        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).When(x => x.CreditLimit.HasValue)
            .WithMessage("Credit limit cannot be negative");

        RuleFor(x => x.CreditLimit)
            .Null()
            .When(x => !string.Equals(x.Type, "CreditCard", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Credit limit is only allowed on credit cards");
    }
}
