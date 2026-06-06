using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Households.RemoveMember;

/// <summary>
/// Removes a member from a household. Owner only; the owner cannot be removed.
/// </summary>
public class RemoveMemberEndpoint : EndpointWithoutRequest
{
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public RemoveMemberEndpoint(
        ICurrentUserService currentUser,
        IHouseholdMemberRepository members,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _members = members;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Delete("/api/v1/households/{id}/members/{memberId}");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 30, durationSeconds: 60);
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

        var householdId = Route<Guid>("id");
        var memberId = Route<Guid>("memberId");

        var callerMembership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (callerMembership is null || callerMembership.HouseholdId != householdId || callerMembership.Role != HouseholdRole.Owner)
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var member = await _members.GetByIdAsync(memberId, ct);
        if (member is null || member.HouseholdId != householdId)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        if (member.Role == HouseholdRole.Owner)
        {
            ThrowError("The household owner cannot be removed.", 400);
            return;
        }

        _members.Delete(member);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
