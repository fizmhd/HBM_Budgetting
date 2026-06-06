using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Transactions.DeleteTransaction;

/// <summary>
/// Deletes a transaction (and, by cascade, its splits and tag joins) if visible to the caller.
/// </summary>
public class DeleteTransactionEndpoint : EndpointWithoutRequest
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITransactionRepository _transactions;
    private readonly IHouseholdMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public DeleteTransactionEndpoint(
        ICurrentUserService currentUser,
        ITransactionRepository transactions,
        IHouseholdMemberRepository members,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _transactions = transactions;
        _members = members;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Delete("/api/v1/transactions/{id}");

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

        var transaction = await _transactions.GetByIdAsync(Route<Guid>("id"), ct);
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

        _transactions.Delete(transaction);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
