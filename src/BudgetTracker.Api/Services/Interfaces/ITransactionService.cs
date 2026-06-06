using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// The fields needed to validate a transaction's structural invariants, independent of the wire DTO.
/// </summary>
public sealed record TransactionValidationInput(
    TransactionType Type,
    decimal Amount,
    Guid AccountId,
    Guid? CounterAccountId,
    IReadOnlyList<TransactionSplitValue> Splits);

/// <summary>A single split's category + amount for validation.</summary>
public sealed record TransactionSplitValue(Guid CategoryId, decimal Amount);

/// <summary>
/// Enforces the transaction invariants (TASK 4.1 / 4.3): positive amount; income/expense carry
/// category splits whose amounts sum to the total; transfers carry no splits and move between two
/// distinct accounts.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Validates the structural invariants. Account visibility and currency matching are enforced
    /// separately by the endpoint (they require repository lookups).
    /// </summary>
    Result Validate(TransactionValidationInput input);
}
