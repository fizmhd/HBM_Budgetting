namespace BudgetTracker.Api.Infrastructure.Email;

/// <summary>
/// MVP <see cref="IEmailSender"/> that writes the message to the application log instead of dialling a
/// real mail provider. This keeps the budget-alert path end-to-end testable and observable without
/// external infrastructure; replace the registration with an SMTP/provider sender when one is wired.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email (logged, not sent) to {Recipient}: {Subject} — {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
