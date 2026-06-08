using BudgetTracker.Api.Features.Transactions;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Payslips;
using BudgetTracker.Shared.DTOs.Transactions;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Features.Payslips;

/// <summary>
/// Posts a payslip (TASK 8.4): turns its net pay into an income transaction on the chosen account.
/// The transaction is created through the shared <see cref="TransactionWriteService"/> so it obeys
/// exactly the same invariants as manual entry (account/category visibility, split sum). The net that
/// lands is the payslip's printed net (<see cref="Payslip.DeclaredNet"/>) — the real cash inflow.
/// </summary>
public sealed class PayslipPostingService
{
    public const string AlreadyPostedCode = "PAYSLIP_ALREADY_POSTED";
    public const string NetNotPositiveCode = "PAYSLIP_NET_NOT_POSITIVE";

    private readonly TransactionWriteService _writer;
    private readonly ITransactionRepository _transactions;

    public PayslipPostingService(TransactionWriteService writer, ITransactionRepository transactions)
    {
        _writer = writer;
        _transactions = transactions;
    }

    /// <summary>
    /// Creates the income transaction and flips the payslip to <see cref="PayslipStatus.Posted"/>.
    /// Caller persists on success. Returns the new transaction id.
    /// </summary>
    public async Task<Result<Guid>> PostAsync(Payslip payslip, PostPayslipRequest req, Guid userId,
        Guid? householdId, CancellationToken ct)
    {
        if (payslip.Status == PayslipStatus.Posted)
        {
            return Result<Guid>.Failure(Error.Conflict(AlreadyPostedCode,
                "This payslip has already been posted."));
        }

        if (payslip.DeclaredNet <= 0)
        {
            return Result<Guid>.Failure(Error.Validation(NetNotPositiveCode,
                "Net pay must be greater than zero to post."));
        }

        var request = new CreateTransactionRequest
        {
            Type = nameof(TransactionType.Income),
            AccountId = req.AccountId,
            Date = req.Date ?? payslip.PayDate,
            Amount = payslip.DeclaredNet,
            CurrencyCode = payslip.CurrencyCode,
            Description = $"Salary – {payslip.EmployerName}",
            IsShared = payslip.Visibility == Visibility.HouseholdShared,
            Splits = new List<TransactionSplitInput>
            {
                new() { CategoryId = req.CategoryId, Amount = payslip.DeclaredNet }
            }
        };

        var transaction = new Transaction { Id = Guid.NewGuid() };
        var result = await _writer.ApplyAsync(transaction, request, userId, householdId, ct);
        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Errors);
        }

        await _transactions.AddAsync(transaction, ct);

        payslip.Status = PayslipStatus.Posted;
        payslip.PostedTransactionId = transaction.Id;
        payslip.PostedAccountId = req.AccountId;
        payslip.PostedAt = DateTime.UtcNow;

        return Result<Guid>.Success(transaction.Id);
    }
}
