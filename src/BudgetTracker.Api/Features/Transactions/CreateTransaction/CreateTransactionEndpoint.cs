using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Transactions;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Transactions.CreateTransaction;

/// <summary>
/// Creates a transaction (income/expense with splits, or a transfer) for the caller.
/// </summary>
public class CreateTransactionEndpoint : Endpoint<CreateTransactionRequest, TransactionDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITransactionRepository _transactions;
    private readonly IHouseholdMemberRepository _members;
    private readonly IAccountRepository _accounts;
    private readonly ICategoryRepository _categories;
    private readonly TransactionWriteService _writer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public CreateTransactionEndpoint(
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
        Post("/api/v1/transactions");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 120, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CreateTransactionRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);

        var transaction = new Transaction { Id = Guid.NewGuid() };
        var result = await _writer.ApplyAsync(transaction, req, userId.Value, membership?.HouseholdId, ct);
        if (result.IsFailure)
        {
            ThrowError(result.Errors[0].Message, 400);
            return;
        }

        await _transactions.AddAsync(transaction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = await BuildDtoAsync(transaction, userId.Value, membership?.HouseholdId, ct);
        await SendOkAsync(dto, ct);
    }

    private async Task<TransactionDto> BuildDtoAsync(Transaction transaction, Guid userId, Guid? householdId,
        CancellationToken ct)
    {
        var accountNames = (await _accounts.GetVisibleAsync(userId, householdId, ct))
            .ToDictionary(a => a.Id, a => a.Name);
        var categoryNames = (await _categories.GetVisibleAsync(userId, householdId, ct))
            .ToDictionary(c => c.Id, c => c.Name);
        return transaction.ToDto(accountNames, categoryNames);
    }
}
