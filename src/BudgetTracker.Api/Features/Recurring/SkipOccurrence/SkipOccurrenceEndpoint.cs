using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Recurring;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Recurring.SkipOccurrence;

/// <summary>
/// Skips a pending occurrence with a required reason (TASK 5.3).
/// </summary>
public class SkipOccurrenceEndpoint : Endpoint<SkipOccurrenceRequest, RecurringOccurrenceDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecurringOccurrenceRepository _occurrences;
    private readonly IRecurringRuleRepository _rules;
    private readonly IHouseholdMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public SkipOccurrenceEndpoint(
        ICurrentUserService currentUser,
        IRecurringOccurrenceRepository occurrences,
        IRecurringRuleRepository rules,
        IHouseholdMemberRepository members,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _occurrences = occurrences;
        _rules = rules;
        _members = members;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/recurring/occurrences/{id}/skip");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(SkipOccurrenceRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(req.Reason))
        {
            ThrowError("A reason is required to skip an occurrence.", 400);
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
            ThrowError("Only a pending occurrence can be skipped.", 400);
            return;
        }

        occurrence.Status = OccurrenceStatus.Skipped;
        occurrence.SkipReason = req.Reason.Trim();
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(occurrence.ToDto(rule), ct);
    }
}
