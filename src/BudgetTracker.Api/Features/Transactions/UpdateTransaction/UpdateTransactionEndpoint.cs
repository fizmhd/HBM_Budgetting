using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Transactions;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Transactions.UpdateTransaction;

/// <summary>
/// Updates an existing transaction (re-validating all invariants).
/// </summary>
public class UpdateTransactionEndpoint : Endpoint<UpdateTransactionRequest, TransactionDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITransactionRepository _transactions;
    private readonly IHouseholdMemberRepository _members;
    private readonly IAccountRepository _accounts;
    private readonly ICategoryRepository _categories;
    private readonly TransactionWriteService _writer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public UpdateTransactionEndpoint(
        ICurrentUserService currentUser,
        ITransactionRepository transactions,
        IHouseholdMemberRepository members,
        IAccountRepository accounts,
        ICategoryRepository categories,
        TransactionWriteService writer,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _transactions = transactions;
        _members = members;
        _accounts = accounts;
        _categories = categories;
        _writer = writer;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Put("/api/v1/transactions/{id}");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 120, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(UpdateTransactionRequest req, CancellationToken ct)
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

        var result = await _writer.ApplyAsync(transaction, req, userId.Value, membership?.HouseholdId, ct);
        if (result.IsFailure)
        {
            ThrowError(result.Errors[0].Message, 400);
            return;
        }

        // The entity was loaded tracked (with its splits/tags), so EF change-tracking persists the
        // scalar edits, orphaned splits, and re-added tag joins. Calling Update() here would wrongly
        // mark the freshly-added child rows as Modified.
        await _unitOfWork.SaveChangesAsync(ct);

        var accountNames = (await _accounts.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct))
            .ToDictionary(a => a.Id, a => a.Name);
        var categoryNames = (await _categories.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct))
            .ToDictionary(c => c.Id, c => c.Name);

        await SendOkAsync(transaction.ToDto(accountNames, categoryNames), ct);
    }
}
