using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Recurring;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Recurring.ConfirmOccurrence;

/// <summary>
/// Confirms a pending occurrence (TASK 5.3): creates the transaction and marks the occurrence Posted.
/// </summary>
public class ConfirmOccurrenceEndpoint : EndpointWithoutRequest<RecurringOccurrenceDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecurringOccurrenceRepository _occurrences;
    private readonly IRecurringRuleRepository _rules;
    private readonly IHouseholdMemberRepository _members;
    private readonly IRecurringGenerationService _generation;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public ConfirmOccurrenceEndpoint(
        ICurrentUserService currentUser,
        IRecurringOccurrenceRepository occurrences,
        IRecurringRuleRepository rules,
        IHouseholdMemberRepository members,
        IRecurringGenerationService generation,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _occurrences = occurrences;
        _rules = rules;
        _members = members;
        _generation = generation;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/recurring/occurrences/{id}/confirm");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var occurrence = await _occurrences.GetByIdAsync(Route<Guid>("id"), ct);
        if (occurrence is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var rule = await _rules.GetByIdAsync(occurrence.RecurringRuleId, ct);
        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (rule is null || !rule.IsVisibleTo(userId.Value, membership?.HouseholdId))
        {
            await SendNotFoundAsync(ct);
            return;
        }

        if (occurrence.Status != OccurrenceStatus.Pending)
        {
            ThrowError("Only a pending occurrence can be confirmed.", 400);
            return;
        }

        // Post the transaction under the rule's owner/visibility (not necessarily the confirming user's).
        var ownerMembership = rule.OwnerUserId == userId.Value
            ? membership
            : await _members.GetByUserIdAsync(rule.OwnerUserId, ct);

        var posted = await _generation.PostTransactionAsync(rule, occurrence.DueDate, ownerMembership?.HouseholdId, ct);
        if (posted.IsFailure)
        {
            ThrowError(posted.Errors[0].Message, 400);
            return;
        }

        occurrence.Status = OccurrenceStatus.Posted;
        occurrence.GeneratedTransactionId = posted.Value;
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(occurrence.ToDto(rule), ct);
    }
}
