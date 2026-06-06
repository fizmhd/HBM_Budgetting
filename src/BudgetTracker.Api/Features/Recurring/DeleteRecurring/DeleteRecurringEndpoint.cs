using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Recurring.DeleteRecurring;

/// <summary>
/// Deletes a recurring rule (and its occurrences, by cascade) if visible to the caller.
/// </summary>
public class DeleteRecurringEndpoint : EndpointWithoutRequest
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecurringRuleRepository _rules;
    private readonly IHouseholdMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public DeleteRecurringEndpoint(
        ICurrentUserService currentUser,
        IRecurringRuleRepository rules,
        IHouseholdMemberRepository members,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _rules = rules;
        _members = members;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Delete("/api/v1/recurring/{id}");

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

        var rule = await _rules.GetByIdAsync(Route<Guid>("id"), ct);
        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (rule is null || !rule.IsVisibleTo(userId.Value, membership?.HouseholdId))
        {
            await SendNotFoundAsync(ct);
            return;
        }

        _rules.Delete(rule);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
