using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Accounts;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Accounts.CreateAccount;

/// <summary>
/// Creates an account owned by the caller, optionally shared with their household.
/// </summary>
public class CreateAccountEndpoint : Endpoint<CreateAccountRequest, AccountDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAccountRepository _accounts;
    private readonly IHouseholdMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public CreateAccountEndpoint(
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
        Post("/api/v1/accounts");
        Validator<CreateAccountRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CreateAccountRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        AccountMapping.TryParseType(req.Type, out var type);

        // Credit limit only applies to credit cards.
        var creditLimit = type == AccountType.CreditCard ? req.CreditLimit : null;

        var membership = req.IsShared ? await _members.GetByUserIdAsync(userId.Value, ct) : null;
        if (req.IsShared && membership is null)
        {
            ThrowError("You must belong to a household to share an account.", 400);
            return;
        }

        var account = new Account
        {
            OwnerUserId = userId.Value,
            Visibility = req.IsShared ? Visibility.HouseholdShared : Visibility.Individual,
            HouseholdId = req.IsShared ? membership!.HouseholdId : null,
            Name = req.Name.Trim(),
            Type = type,
            CurrencyCode = req.CurrencyCode.ToUpperInvariant(),
            OpeningBalance = req.OpeningBalance,
            CreditLimit = creditLimit,
            IsArchived = false
        };

        await _accounts.AddAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(account.ToDto(), ct);
    }
}
