using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Households;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Households.GetCurrentHousehold;

/// <summary>
/// Returns the caller's household and its members.
/// </summary>
public class GetCurrentHouseholdEndpoint : EndpointWithoutRequest<HouseholdDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdRepository _households;
    private readonly IHouseholdMemberRepository _members;
    private readonly IWebHostEnvironment _environment;

    public GetCurrentHouseholdEndpoint(
        ICurrentUserService currentUser,
        IHouseholdRepository households,
        IHouseholdMemberRepository members,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _households = households;
        _members = members;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/households/current");

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
        if (membership is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var household = await _households.GetWithMembersAsync(membership.HouseholdId, ct);
        if (household is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendOkAsync(household.ToDto(household.Members), ct);
    }
}
