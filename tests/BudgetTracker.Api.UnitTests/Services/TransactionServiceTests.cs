using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services;
using BudgetTracker.Api.Services.Interfaces;
using FluentAssertions;

namespace BudgetTracker.Api.UnitTests.Services;

/// <summary>
/// Unit tests for the transaction structural invariants (TASK 4.1 / 4.3).
/// </summary>
public class TransactionServiceTests
{
    private readonly TransactionService _service = new();

    private static readonly Guid AccountA = Guid.NewGuid();
    private static readonly Guid AccountB = Guid.NewGuid();
    private static readonly Guid Category = Guid.NewGuid();

    private static TransactionValidationInput Expense(decimal amount, params TransactionSplitValue[] splits) =>
        new(TransactionType.Expense, amount, AccountA, null, splits);

    [Fact]
    public void Single_category_expense_with_matching_split_is_valid()
    {
        var result = _service.Validate(Expense(100m, new TransactionSplitValue(Category, 100m)));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Multi_split_summing_to_amount_is_valid()
    {
        var result = _service.Validate(Expense(100m,
            new TransactionSplitValue(Category, 60m),
            new TransactionSplitValue(Guid.NewGuid(), 40m)));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Split_sum_not_equal_to_amount_is_rejected()
    {
        var result = _service.Validate(Expense(100m,
            new TransactionSplitValue(Category, 60m),
            new TransactionSplitValue(Guid.NewGuid(), 30m)));

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionService.SplitSumCode);
    }

    [Fact]
    public void Non_positive_amount_is_rejected()
    {
        var result = _service.Validate(Expense(0m, new TransactionSplitValue(Category, 0m)));
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionService.AmountPositiveCode);
    }

    [Fact]
    public void Income_or_expense_without_splits_is_rejected()
    {
        var result = _service.Validate(Expense(50m));
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionService.SplitRequiredCode);
    }

    [Fact]
    public void Split_without_a_category_is_rejected()
    {
        var result = _service.Validate(Expense(50m, new TransactionSplitValue(Guid.Empty, 50m)));
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionService.SplitCategoryCode);
    }

    [Fact]
    public void Account_less_expense_is_valid()
    {
        // Income/expense may be recorded without an account ("cash") — TASK 4.1.
        var result = _service.Validate(
            new TransactionValidationInput(TransactionType.Expense, 100m, null, null,
                new[] { new TransactionSplitValue(Category, 100m) }));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Transfer_without_a_source_account_is_rejected()
    {
        // A transfer is by definition a move between two accounts, so it can never be account-less.
        var result = _service.Validate(
            new TransactionValidationInput(TransactionType.Transfer, 100m, null, AccountB,
                Array.Empty<TransactionSplitValue>()));
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionService.TransferAccountsCode);
    }

    [Fact]
    public void Valid_transfer_between_two_accounts_is_valid()
    {
        var result = _service.Validate(
            new TransactionValidationInput(TransactionType.Transfer, 100m, AccountA, AccountB,
                Array.Empty<TransactionSplitValue>()));
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Transfer_to_the_same_account_is_rejected()
    {
        var result = _service.Validate(
            new TransactionValidationInput(TransactionType.Transfer, 100m, AccountA, AccountA,
                Array.Empty<TransactionSplitValue>()));
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionService.TransferAccountsCode);
    }

    [Fact]
    public void Transfer_with_splits_is_rejected()
    {
        var result = _service.Validate(
            new TransactionValidationInput(TransactionType.Transfer, 100m, AccountA, AccountB,
                new[] { new TransactionSplitValue(Category, 100m) }));
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionService.TransferNoSplitsCode);
    }

    [Fact]
    public void Income_or_expense_with_a_counter_account_is_rejected()
    {
        var result = _service.Validate(
            new TransactionValidationInput(TransactionType.Expense, 100m, AccountA, AccountB,
                new[] { new TransactionSplitValue(Category, 100m) }));
        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(TransactionService.TransferAccountsCode);
    }
}
