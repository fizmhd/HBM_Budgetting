using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Services;

/// <summary>
/// Implements the transaction structural invariants (TASK 4.1 / 4.3). Pure and unit-testable.
/// </summary>
public sealed class TransactionService : ITransactionService
{
    public const string AmountPositiveCode = "TRANSACTION_AMOUNT_INVALID";
    public const string SplitSumCode = "TRANSACTION_SPLIT_SUM_MISMATCH";
    public const string SplitRequiredCode = "TRANSACTION_SPLIT_REQUIRED";
    public const string SplitCategoryCode = "TRANSACTION_SPLIT_CATEGORY_REQUIRED";
    public const string TransferAccountsCode = "TRANSACTION_TRANSFER_ACCOUNTS_INVALID";
    public const string TransferNoSplitsCode = "TRANSACTION_TRANSFER_NO_SPLITS";

    /// <inheritdoc />
    public Result Validate(TransactionValidationInput input)
    {
        if (input.Amount <= 0)
        {
            return Result.Failure(Error.Validation(AmountPositiveCode, "Amount must be greater than zero."));
        }

        return input.Type == TransactionType.Transfer
            ? ValidateTransfer(input)
            : ValidateCategorised(input);
    }

    private static Result ValidateTransfer(TransactionValidationInput input)
    {
        if (input.Splits.Count > 0)
        {
            return Result.Failure(Error.Validation(TransferNoSplitsCode,
                "Transfers move money between accounts and cannot have category splits."));
        }

        // A transfer is an account-to-account move, so unlike income/expense it can never be
        // account-less: both the source and a distinct destination are required (TASK 4.3).
        if (input.AccountId is null || input.CounterAccountId is null ||
            input.CounterAccountId == input.AccountId)
        {
            return Result.Failure(Error.Validation(TransferAccountsCode,
                "A transfer must go between two different accounts."));
        }

        return Result.Success();
    }

    private static Result ValidateCategorised(TransactionValidationInput input)
    {
        if (input.CounterAccountId is not null)
        {
            return Result.Failure(Error.Validation(TransferAccountsCode,
                "Only transfers may set a destination account."));
        }

        if (input.Splits.Count == 0)
        {
            return Result.Failure(Error.Validation(SplitRequiredCode,
                "Income and expenses need at least one category split."));
        }

        foreach (var split in input.Splits)
        {
            if (split.CategoryId == Guid.Empty)
            {
                return Result.Failure(Error.Validation(SplitCategoryCode,
                    "Every split must reference a category."));
            }

            if (split.Amount <= 0)
            {
                return Result.Failure(Error.Validation(AmountPositiveCode,
                    "Each split amount must be greater than zero."));
            }
        }

        var sum = input.Splits.Sum(s => s.Amount);
        if (sum != input.Amount)
        {
            return Result.Failure(Error.Validation(SplitSumCode,
                $"Split amounts ({sum:0.##}) must add up to the transaction amount ({input.Amount:0.##})."));
        }

        return Result.Success();
    }
}
