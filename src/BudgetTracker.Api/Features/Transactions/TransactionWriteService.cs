using BudgetTracker.Api.Features.Accounts;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Transactions;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Features.Transactions;

/// <summary>
/// Shared create/update logic for transactions: resolves and authorises accounts/categories/tags,
/// runs the structural invariants, and mutates the entity. Used by both the create and update slices
/// so the rules live in exactly one place.
/// </summary>
public sealed class TransactionWriteService
{
    public const string AccountNotFoundCode = "TRANSACTION_ACCOUNT_NOT_FOUND";
    public const string CategoryNotFoundCode = "TRANSACTION_CATEGORY_NOT_FOUND";
    public const string CurrencyMismatchCode = "TRANSACTION_CURRENCY_MISMATCH";
    public const string SharedRequiresHouseholdCode = "TRANSACTION_SHARED_REQUIRES_HOUSEHOLD";

    private readonly IAccountRepository _accounts;
    private readonly ICategoryRepository _categories;
    private readonly ITagRepository _tags;
    private readonly ITransactionRepository _transactions;
    private readonly ITransactionService _invariants;

    public TransactionWriteService(
        IAccountRepository accounts,
        ICategoryRepository categories,
        ITagRepository tags,
        ITransactionRepository transactions,
        ITransactionService invariants)
    {
        _accounts = accounts;
        _categories = categories;
        _tags = tags;
        _transactions = transactions;
        _invariants = invariants;
    }

    /// <summary>
    /// Applies <paramref name="req"/> onto <paramref name="transaction"/> (a fresh or existing entity).
    /// Caller persists on success. Owner/visibility fields are set here.
    /// </summary>
    public async Task<Result> ApplyAsync(Transaction transaction, CreateTransactionRequest req,
        Guid userId, Guid? householdId, CancellationToken ct)
    {
        if (!Enum.TryParse<TransactionType>(req.Type, ignoreCase: true, out var type) || !Enum.IsDefined(type))
        {
            return Result.Failure(Error.Validation("TRANSACTION_TYPE_INVALID",
                "Type must be Income, Expense, or Transfer."));
        }

        if (req.IsShared && householdId is null)
        {
            return Result.Failure(Error.Validation(SharedRequiresHouseholdCode,
                "You must belong to a household to share a transaction."));
        }

        // Source account must be visible.
        var account = await _accounts.GetByIdAsync(req.AccountId, ct);
        if (account is null || !account.IsVisibleTo(userId, householdId))
        {
            return Result.Failure(Error.Validation(AccountNotFoundCode, "Account not found."));
        }

        Account? counterAccount = null;
        if (type == TransactionType.Transfer)
        {
            if (req.CounterAccountId is not { } counterId)
            {
                return Result.Failure(Error.Validation(TransactionService.TransferAccountsCode,
                    "A transfer must go between two different accounts."));
            }

            counterAccount = await _accounts.GetByIdAsync(counterId, ct);
            if (counterAccount is null || !counterAccount.IsVisibleTo(userId, householdId))
            {
                return Result.Failure(Error.Validation(AccountNotFoundCode, "Destination account not found."));
            }

            if (!string.Equals(account.CurrencyCode, counterAccount.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure(Error.Validation(CurrencyMismatchCode,
                    "Transfers must be between accounts of the same currency."));
            }
        }

        // Structural invariants (amount, split sum, transfer rules).
        var splitValues = req.Splits
            .Select(s => new TransactionSplitValue(s.CategoryId, s.Amount))
            .ToList();
        var invariant = _invariants.Validate(
            new TransactionValidationInput(type, req.Amount, req.AccountId, req.CounterAccountId, splitValues));
        if (invariant.IsFailure)
        {
            return invariant;
        }

        // Every split category must be visible to the caller.
        if (type != TransactionType.Transfer)
        {
            var visibleCategoryIds = (await _categories.GetVisibleAsync(userId, householdId, ct))
                .Select(c => c.Id)
                .ToHashSet();
            if (req.Splits.Any(s => !visibleCategoryIds.Contains(s.CategoryId)))
            {
                return Result.Failure(Error.Validation(CategoryNotFoundCode, "One or more categories were not found."));
            }
        }

        // Resolve/create tags (scoped per owner).
        var tags = await ResolveTagsAsync(req.Tags, userId, householdId, req.IsShared, ct);

        // ---- Mutate the entity ----
        transaction.OwnerUserId = userId;
        transaction.Visibility = req.IsShared ? Visibility.HouseholdShared : Visibility.Individual;
        transaction.HouseholdId = req.IsShared ? householdId : null;
        transaction.AccountId = req.AccountId;
        transaction.Date = req.Date;
        transaction.Type = type;
        transaction.Amount = req.Amount;
        transaction.CurrencyCode = account.CurrencyCode;
        transaction.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        transaction.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
        transaction.CounterAccountId = type == TransactionType.Transfer ? req.CounterAccountId : null;

        // Replace splits. On update the old rows are tracked, so mark them for deletion explicitly
        // (clearing the navigation alone leaves EF unsure how to treat the now-orphaned rows).
        if (transaction.Splits.Count > 0)
        {
            _transactions.RemoveSplits(transaction.Splits.ToList());
            transaction.Splits.Clear();
        }
        if (type != TransactionType.Transfer)
        {
            foreach (var s in req.Splits)
            {
                // Leave Id unset (default Guid) so EF treats the split as a new row to INSERT — a
                // navigation-discovered child with a non-default key would be taken for an existing row.
                transaction.Splits.Add(new TransactionSplit
                {
                    CategoryId = s.CategoryId,
                    Amount = s.Amount,
                    Note = string.IsNullOrWhiteSpace(s.Note) ? null : s.Note.Trim()
                });
            }
        }

        // Replace tag joins. A join's composite key is always set, so they must be registered as
        // Added explicitly (otherwise EF assumes they already exist and issues a no-op UPDATE).
        if (transaction.TransactionTags.Count > 0)
        {
            _transactions.RemoveTags(transaction.TransactionTags.ToList());
            transaction.TransactionTags.Clear();
        }
        var joins = tags
            .Select(tag => new TransactionTag { TransactionId = transaction.Id, TagId = tag.Id, Tag = tag })
            .ToList();
        foreach (var join in joins)
        {
            transaction.TransactionTags.Add(join);
        }
        if (joins.Count > 0)
        {
            _transactions.AddTags(joins);
        }

        return Result.Success();
    }

    private async Task<List<Tag>> ResolveTagsAsync(IEnumerable<string> rawNames, Guid userId, Guid? householdId,
        bool isShared, CancellationToken ct)
    {
        var names = rawNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (names.Count == 0)
        {
            return new List<Tag>();
        }

        var existing = await _tags.GetByNamesAsync(userId, names, ct);
        var byName = existing.ToDictionary(t => t.Name);
        var result = new List<Tag>();

        foreach (var name in names)
        {
            if (byName.TryGetValue(name, out var tag))
            {
                result.Add(tag);
            }
            else
            {
                var created = new Tag
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    OwnerUserId = userId,
                    Visibility = isShared ? Visibility.HouseholdShared : Visibility.Individual,
                    HouseholdId = isShared ? householdId : null
                };
                await _tags.AddAsync(created, ct);
                result.Add(created);
            }
        }

        return result;
    }
}
