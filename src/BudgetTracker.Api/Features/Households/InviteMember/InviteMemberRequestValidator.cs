using BudgetTracker.Shared.DTOs.Households;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Households.InviteMember;

/// <summary>
/// Validator for inviting a household member.
/// </summary>
public class InviteMemberRequestValidator : Validator<InviteMemberRequest>
{
    public InviteMemberRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("A valid email address is required")
            .MaximumLength(256)
            .WithMessage("Email cannot exceed 256 characters");
    }
}
