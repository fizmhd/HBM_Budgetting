using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Accounts;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Accounts.UpdateAccount;

/// <summary>
/// Updates an account's name, type, credit limit, and sharing.
/// </summary>
public class UpdateAccountEndpoint : Endpoint<UpdateAccountRequest, AccountDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAccountRepository _accounts;
    private readonly IHouseholdMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public UpdateAccountEndpoint(
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
        Put("/api/v1/accounts/{id}");
        Validator<UpdateAccountRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(UpdateAccountRequest req, CancellationToken ct)
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

        if (req.IsShared && membership is null)
        {
            ThrowError("You must belong to a household to share an account.", 400);
            return;
        }

        AccountMapping.TryParseType(req.Type, out var type);

        account.Name = req.Name.Trim();
        account.Type = type;
        account.CreditLimit = type == AccountType.CreditCard ? req.CreditLimit : null;
        account.Visibility = req.IsShared ? Visibility.HouseholdShared : Visibility.Individual;
        account.HouseholdId = req.IsShared ? membership!.HouseholdId : null;

        _accounts.Update(account);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(account.ToDto(), ct);
    }
}
