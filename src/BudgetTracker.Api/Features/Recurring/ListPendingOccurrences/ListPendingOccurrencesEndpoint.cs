using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Recurring;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Recurring.ListPendingOccurrences;

/// <summary>
/// Lists the pending occurrences awaiting confirmation for rules visible to the caller (TASK 5.3/5.5).
/// </summary>
public class ListPendingOccurrencesEndpoint : EndpointWithoutRequest<List<RecurringOccurrenceDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecurringOccurrenceRepository _occurrences;
    private readonly IHouseholdMemberRepository _members;
    private readonly IWebHostEnvironment _environment;

    public ListPendingOccurrencesEndpoint(
        ICurrentUserService currentUser,
        IRecurringOccurrenceRepository occurrences,
        IHouseholdMemberRepository members,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _occurrences = occurrences;
        _members = members;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/recurring/occurrences/pending");

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

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        var pending = await _occurrences.GetPendingVisibleAsync(userId.Value, membership?.HouseholdId, ct);

        var dtos = pending.Select(p => p.Occurrence.ToDto(p.Rule)).ToList();
        await SendOkAsync(dtos, ct);
    }
}
