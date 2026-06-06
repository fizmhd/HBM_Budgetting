namespace BudgetTracker.Api.Infrastructure.Email;

/// <summary>
/// Minimal transactional-email seam for the application's own notifications (e.g. budget alerts).
/// Auth emails (confirm/reset) are handled by the external auth provider; this abstraction exists so
/// features can send mail without taking a hard dependency on a concrete provider. The MVP ships a
/// logging implementation; a real SMTP/provider sender can be swapped in via DI without code changes.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends a plain email. Implementations should be resilient — a failure to deliver a notification
    /// must not break the originating request.
    /// </summary>
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
