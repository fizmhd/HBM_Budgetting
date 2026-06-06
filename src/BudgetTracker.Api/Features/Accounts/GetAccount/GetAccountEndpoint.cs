using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Accounts;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Accounts.GetAccount;

/// <summary>
/// Returns a single account (with its derived balance) if it is visible to the caller.
/// </summary>
public class GetAccountEndpoint : EndpointWithoutRequest<AccountDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAccountRepository _accounts;
    private readonly IHouseholdMemberRepository _members;
    private readonly IBalanceService _balances;
    private readonly IWebHostEnvironment _environment;

    public GetAccountEndpoint(
        ICurrentUserService currentUser,
        IAccountRepository accounts,
        IHouseholdMemberRepository members,
        IBalanceService balances,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _accounts = accounts;
        _members = members;
        _balances = balances;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/accounts/{id}");

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

        var account = await _accounts.GetByIdAsync(Route<Guid>("id"), ct);
        if (account is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (!account.IsVisibleTo(userId.Value, membership?.HouseholdId))
        {
            // Don't reveal existence of records the caller can't see.
            await SendNotFoundAsync(ct);
            return;
        }

        var balances = await _balances.GetBalancesAsync(userId.Value, membership?.HouseholdId, new[] { account }, ct);

        await SendOkAsync(account.ToDto(balances[account.Id]), ct);
    }
}
