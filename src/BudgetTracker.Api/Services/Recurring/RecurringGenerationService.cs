using BudgetTracker.Api.Features.Transactions;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Transactions;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Services.Recurring;

/// <summary>
/// Default <see cref="IRecurringGenerationService"/> (TASK 5.2). Transactions are created through the
/// shared <see cref="TransactionWriteService"/> so generated entries obey exactly the same invariants
/// as manual entry. A failed AutoPost (e.g. the category was later archived) becomes a Pending
/// occurrence so nothing is silently lost and the loop never stalls on a bad date.
/// </summary>
public sealed class RecurringGenerationService : IRecurringGenerationService
{
    // Guards against an accidental runaway catch-up (e.g. a daily rule started years ago).
    private const int MaxCatchUpPerRule = 1000;

    private readonly IRecurringRuleRepository _rules;
    private readonly IRecurringOccurrenceRepository _occurrences;
    private readonly ITransactionRepository _transactions;
    private readonly IHouseholdMemberRepository _members;
    private readonly TransactionWriteService _writer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RecurringGenerationService> _logger;

    public RecurringGenerationService(
        IRecurringRuleRepository rules,
        IRecurringOccurrenceRepository occurrences,
        ITransactionRepository transactions,
        IHouseholdMemberRepository members,
        TransactionWriteService writer,
        IUnitOfWork unitOfWork,
        ILogger<RecurringGenerationService> logger)
    {
        _rules = rules;
        _occurrences = occurrences;
        _transactions = transactions;
        _members = members;
        _writer = writer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> GenerateDueAsync(DateOnly asOf, Guid? ownerFilter,
        CancellationToken cancellationToken = default)
    {
        var dueRules = await _rules.GetDueAsync(asOf, ownerFilter, cancellationToken);
        var generated = 0;

        foreach (var rule in dueRules)
        {
            var membership = await _members.GetByUserIdAsync(rule.OwnerUserId, cancellationToken);
            var householdId = membership?.HouseholdId;

            var guard = 0;
            while (rule.Status == RecurringStatus.Active &&
                   rule.NextDueDate <= asOf &&
                   (rule.EndDate is null || rule.NextDueDate <= rule.EndDate) &&
                   guard++ < MaxCatchUpPerRule)
            {
                var dueDate = rule.NextDueDate;

                if (!await _occurrences.ExistsAsync(rule.Id, dueDate, cancellationToken))
                {
                    var occurrence = new RecurringOccurrence
                    {
                        Id = Guid.NewGuid(),
                        RecurringRuleId = rule.Id,
                        DueDate = dueDate,
                        Status = OccurrenceStatus.Pending
                    };

                    if (rule.GenerationMode == GenerationMode.AutoPost)
                    {
                        var posted = await PostTransactionAsync(rule, dueDate, householdId, cancellationToken);
                        if (posted.IsSuccess)
                        {
                            occurrence.Status = OccurrenceStatus.Posted;
                            occurrence.GeneratedTransactionId = posted.Value;
                        }
                        else
                        {
                            // Leave it Pending so the owner can fix and confirm it later.
                            _logger.LogWarning("Auto-post failed for rule {RuleId} on {DueDate}: {Error}",
                                rule.Id, dueDate, posted.Errors[0].Message);
                        }
                    }

                    await _occurrences.AddAsync(occurrence, cancellationToken);
                    generated++;
                }

                rule.NextDueDate = RecurrenceCalculator.Next(
                    rule.NextDueDate, rule.Frequency, rule.Interval, rule.DayOfMonth);
            }
        }

        if (generated > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return generated;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> PostTransactionAsync(RecurringRule rule, DateOnly dueDate,
        Guid? householdId, CancellationToken cancellationToken = default)
    {
        var request = new CreateTransactionRequest
        {
            Type = rule.Type.ToString(),
            AccountId = rule.AccountId,
            Date = dueDate,
            Amount = rule.Amount,
            CurrencyCode = rule.CurrencyCode,
            Description = rule.Name,
            IsShared = rule.Visibility == Visibility.HouseholdShared,
            Splits = rule.CategoryId is { } categoryId
                ? new List<TransactionSplitInput> { new() { CategoryId = categoryId, Amount = rule.Amount } }
                : new List<TransactionSplitInput>()
        };

        var transaction = new Transaction { Id = Guid.NewGuid() };
        var result = await _writer.ApplyAsync(transaction, request, rule.OwnerUserId, householdId, cancellationToken);
        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Errors);
        }

        await _transactions.AddAsync(transaction, cancellationToken);
        return Result<Guid>.Success(transaction.Id);
    }
}
