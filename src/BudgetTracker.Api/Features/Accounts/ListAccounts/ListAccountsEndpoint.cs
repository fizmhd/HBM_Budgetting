using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Accounts;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Accounts.ListAccounts;

/// <summary>
/// Lists the accounts visible to the caller (own + household-shared) with derived balances.
/// </summary>
public class ListAccountsEndpoint : EndpointWithoutRequest<List<AccountDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAccountRepository _accounts;
    private readonly IHouseholdMemberRepository _members;
    private readonly IBalanceService _balances;
    private readonly IWebHostEnvironment _environment;

    public ListAccountsEndpoint(
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
        Get("/api/v1/accounts");

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
        var accounts = await _accounts.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct);
        var balances = await _balances.GetBalancesAsync(userId.Value, membership?.HouseholdId, accounts, ct);

        await SendOkAsync(accounts.Select(a => a.ToDto(balances[a.Id])).ToList(), ct);
    }
}
