using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Transactions;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Transactions.GetTransaction;

/// <summary>
/// Returns a single transaction (with splits and tags) if visible to the caller.
/// </summary>
public class GetTransactionEndpoint : EndpointWithoutRequest<TransactionDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITransactionRepository _transactions;
    private readonly IHouseholdMemberRepository _members;
    private readonly IAccountRepository _accounts;
    private readonly ICategoryRepository _categories;
    private readonly IWebHostEnvironment _environment;

    public GetTransactionEndpoint(
        ICurrentUserService currentUser,
        ITransactionRepository transactions,
        IHouseholdMemberRepository members,
        IAccountRepository accounts,
        ICategoryRepository categories,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _transactions = transactions;
        _members = members;
        _accounts = accounts;
        _categories = categories;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/transactions/{id}");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 120, durationSeconds: 60);
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

        var transaction = await _transactions.GetWithDetailsAsync(Route<Guid>("id"), ct);
        if (transaction is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (!transaction.IsVisibleTo(userId.Value, membership?.HouseholdId))
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var accountNames = (await _accounts.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct))
            .ToDictionary(a => a.Id, a => a.Name);
        var categoryNames = (await _categories.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct))
            .ToDictionary(c => c.Id, c => c.Name);

        await SendOkAsync(transaction.ToDto(accountNames, categoryNames), ct);
    }
}
