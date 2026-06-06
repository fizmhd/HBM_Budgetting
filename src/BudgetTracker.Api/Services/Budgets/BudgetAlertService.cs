using BudgetTracker.Api.Infrastructure.Email;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services.Budgets;

/// <summary>
/// Default <see cref="IBudgetAlertService"/> (TASK 6.3). Compares each budget's current alert level
/// against the last level we emailed about: a strictly higher level (OK→Warning, Warning→Exceeded)
/// triggers exactly one email and bumps <c>LastAlertedThreshold</c>; falling back to OK resets it so a
/// future re-crossing alerts again. The email is logged via <see cref="IEmailSender"/> (MVP path).
/// </summary>
public sealed class BudgetAlertService : IBudgetAlertService
{
    private readonly IEmailSender _email;
    private readonly IUserRepository _users;
    private readonly ICategoryRepository _categories;
    private readonly ILogger<BudgetAlertService> _logger;

    public BudgetAlertService(
        IEmailSender email,
        IUserRepository users,
        ICategoryRepository categories,
        ILogger<BudgetAlertService> logger)
    {
        _email = email;
        _users = users;
        _categories = categories;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ProcessAsync(IReadOnlyCollection<BudgetEvaluation> evaluations,
        Guid userId, Guid? householdId, CancellationToken cancellationToken = default)
    {
        var changed = false;
        string? recipient = null; // resolved lazily, only when we actually need to send.
        Dictionary<Guid, string>? categoryNames = null;

        foreach (var (budget, progress) in evaluations)
        {
            var level = BudgetCalculator.AlertLevel(progress, budget.AlertThresholdPercent);

            // Usage dropped back under the threshold: clear the marker so a later crossing re-alerts.
            if (level == 0)
            {
                if (budget.LastAlertedThreshold != 0)
                {
                    budget.LastAlertedThreshold = 0;
                    changed = true;
                }
                continue;
            }

            // Already alerted at this level (or higher) for the current run of spending — stay quiet.
            if (level <= budget.LastAlertedThreshold)
            {
                continue;
            }

            recipient ??= await ResolveRecipientAsync(userId, cancellationToken);
            categoryNames ??= (await _categories.GetVisibleAsync(userId, householdId, cancellationToken))
                .ToDictionary(c => c.Id, c => c.Name);

            var categoryName = categoryNames.GetValueOrDefault(budget.CategoryId, "your category");
            var (subject, body) = BuildMessage(progress, level, categoryName, budget.Amount);

            try
            {
                await _email.SendAsync(recipient, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                // A notification failure must never break the read that triggered it.
                _logger.LogWarning(ex, "Failed to send budget alert for budget {BudgetId}", budget.Id);
            }

            budget.LastAlertedThreshold = level;
            changed = true;
        }

        return changed;
    }

    private async Task<string> ResolveRecipientAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user?.Email ?? string.Empty;
    }

    private static (string Subject, string Body) BuildMessage(BudgetProgress progress, int level,
        string categoryName, decimal amount)
    {
        return level >= 100
            ? ($"Budget exceeded: {categoryName}",
               $"You've spent {progress.Spent:N2} of your {amount:N2} budget for {categoryName} " +
               $"({progress.PercentUsed:N0}%). You are over the limit.")
            : ($"Budget alert: {categoryName}",
               $"You've used {progress.PercentUsed:N0}% of your {amount:N2} budget for {categoryName} " +
               $"({progress.Spent:N2} spent, {progress.Remaining:N2} remaining).");
    }
}
