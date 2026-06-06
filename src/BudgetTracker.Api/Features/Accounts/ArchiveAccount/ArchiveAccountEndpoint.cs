using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Accounts;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Accounts.ArchiveAccount;

/// <summary>
/// Toggles an account's archived state.
/// </summary>
public class ArchiveAccountEndpoint : EndpointWithoutRequest<AccountDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAccountRepository _accounts;
    private readonly IHouseholdMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public ArchiveAccountEndpoint(
        ICurrentUserService currentUser,
        IAccountRepository accounts,
        IHouseholdMemberRepository members,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _accounts = accounts;
        _members = members;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/accounts/{id}/archive");

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
            await SendNotFoundAsync(ct);
            return;
        }

        account.IsArchived = !account.IsArchived;
        _accounts.Update(account);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(account.ToDto(), ct);
    }
}
