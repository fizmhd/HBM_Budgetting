using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Transactions;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Transactions.ListTransactions;

/// <summary>
/// Lists the transactions visible to the caller with filters, sorting, and paging (TASK 4.5).
/// </summary>
public class ListTransactionsEndpoint : EndpointWithoutRequest<TransactionListResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITransactionRepository _transactions;
    private readonly IHouseholdMemberRepository _members;
    private readonly IAccountRepository _accounts;
    private readonly ICategoryRepository _categories;
    private readonly IWebHostEnvironment _environment;

    public ListTransactionsEndpoint(
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
        Get("/api/v1/transactions");

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

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);

        var sort = Query<string>("sort", isRequired: false) ?? "date";
        var page = Query<int?>("page", isRequired: false) ?? 1;
        var pageSize = Query<int?>("pageSize", isRequired: false) ?? 25;

        TransactionType? type = null;
        var typeRaw = Query<string>("type", isRequired: false);
        if (!string.IsNullOrWhiteSpace(typeRaw) &&
            Enum.TryParse<TransactionType>(typeRaw, ignoreCase: true, out var parsedType) &&
            Enum.IsDefined(parsedType))
        {
            type = parsedType;
        }

        var filter = new TransactionListFilter
        {
            From = ParseDate(Query<string>("from", isRequired: false)),
            To = ParseDate(Query<string>("to", isRequired: false)),
            AccountId = Query<Guid?>("accountId", isRequired: false),
            NoAccount = Query<bool?>("noAccount", isRequired: false) ?? false,
            CategoryId = Query<Guid?>("categoryId", isRequired: false),
            Type = type,
            Tag = Query<string>("tag", isRequired: false),
            Search = Query<string>("search", isRequired: false),
            // Amount sorts ascending by default (smallest first); date sorts descending (newest first).
            Sort = sort,
            Descending = !string.Equals(sort, "amount", StringComparison.OrdinalIgnoreCase),
            Page = page,
            PageSize = pageSize
        };

        var result = await _transactions.ListAsync(userId.Value, membership?.HouseholdId, filter, ct);

        var accountNames = (await _accounts.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct))
            .ToDictionary(a => a.Id, a => a.Name);
        var categoryNames = (await _categories.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct))
            .ToDictionary(c => c.Id, c => c.Name);

        await SendOkAsync(new TransactionListResponse
        {
            Items = result.Items.Select(t => t.ToDto(accountNames, categoryNames)).ToList(),
            TotalCount = result.TotalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        }, ct);
    }

    private static DateOnly? ParseDate(string? raw) =>
        DateOnly.TryParse(raw, out var date) ? date : null;
}
