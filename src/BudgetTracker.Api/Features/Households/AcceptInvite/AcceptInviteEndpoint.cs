using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Households;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Households.AcceptInvite;

/// <summary>
/// Accepts a household invite by token; the authenticated caller becomes a Member.
/// </summary>
public class AcceptInviteEndpoint : EndpointWithoutRequest<HouseholdDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdMemberRepository _members;
    private readonly IHouseholdInviteRepository _invites;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public AcceptInviteEndpoint(
        ICurrentUserService currentUser,
        IHouseholdRepository households,
        IHouseholdMemberRepository members,
        IHouseholdInviteRepository invites,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _households = households;
        _members = members;
        _invites = invites;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/invites/{token}/accept");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 30, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = await _currentUser.GetUserAsync(ct);
        if (user is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var token = Route<string>("token");
        var invite = await _invites.GetByTokenAsync(token!, ct);
        if (invite is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        if (invite.Status != InviteStatus.Pending)
        {
            ThrowError("This invite is no longer valid.", 409);
            return;
        }

        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            invite.Status = InviteStatus.Expired;
            _invites.Update(invite);
            await _unitOfWork.SaveChangesAsync(ct);
            ThrowError("This invite has expired.", 400);
            return;
        }

        // MVP: a user belongs to at most one household.
        var existing = await _members.GetByUserIdAsync(user.Id, ct);
        if (existing is not null)
        {
            ThrowError("You already belong to a household.", 409);
            return;
        }

        var now = DateTime.UtcNow;
        await _members.AddAsync(new HouseholdMember
        {
            HouseholdId = invite.HouseholdId,
            UserId = user.Id,
            DisplayName = HouseholdMapping.DisplayNameFor(user),
            Role = HouseholdRole.Member,
            JoinedAt = now
        }, ct);

        invite.Status = InviteStatus.Accepted;
        _invites.Update(invite);

        await _unitOfWork.SaveChangesAsync(ct);

        var household = await _households.GetWithMembersAsync(invite.HouseholdId, ct);
        if (household is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(household.ToDto(household.Members), ct);
    }
}
