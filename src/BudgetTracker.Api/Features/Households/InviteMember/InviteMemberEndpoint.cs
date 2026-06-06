using System.Security.Cryptography;
using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Households;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Households.InviteMember;

/// <summary>
/// Invites someone to a household by email. Owner only.
/// Returns a pending invite carrying a shareable token the invitee uses to join.
/// </summary>
public class InviteMemberEndpoint : Endpoint<InviteMemberRequest, HouseholdInviteDto>
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdMemberRepository _members;
    private readonly IHouseholdInviteRepository _invites;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public InviteMemberEndpoint(
        ICurrentUserService currentUser,
        IHouseholdMemberRepository members,
        IHouseholdInviteRepository invites,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _members = members;
        _invites = invites;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/households/{id}/invites");
        Validator<InviteMemberRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 30, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(InviteMemberRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var householdId = Route<Guid>("id");

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (membership is null || membership.HouseholdId != householdId || membership.Role != HouseholdRole.Owner)
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var invite = new HouseholdInvite
        {
            HouseholdId = householdId,
            Email = req.Email.Trim().ToLowerInvariant(),
            Token = GenerateToken(),
            Status = InviteStatus.Pending,
            ExpiresAt = DateTime.UtcNow.Add(InviteLifetime)
        };

        await _invites.AddAsync(invite, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(invite.ToDto(), ct);
    }

    private static string GenerateToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
