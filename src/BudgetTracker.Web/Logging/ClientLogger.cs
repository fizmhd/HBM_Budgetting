using Microsoft.Extensions.Options;

namespace BudgetTracker.Web.Logging;

/// <summary>
/// Console-based implementation of client-side logger.
/// </summary>
public class ClientLogger : IClientLogger
{
    private readonly ClientLoggerOptions _options;
    private string? _correlationId;

    public ClientLogger(IOptions<ClientLoggerOptions> options)
    {
        _options = options.Value;
    }

    public void Debug(string message, params object[] args)
    {
        Log(LogLevel.Debug, message, null, args);
    }

    public void Info(string message, params object[] args)
    {
        Log(LogLevel.Information, message, null, args);
    }

    public void Warning(string message, params object[] args)
    {
        Log(LogLevel.Warning, message, null, args);
    }

    public void Error(string message, Exception? ex = null, params object[] args)
    {
        Log(LogLevel.Error, message, ex, args);
    }

    public void SetCorrelationId(string? correlationId)
    {
        _correlationId = correlationId;
    }

    private void Log(LogLevel level, string message, Exception? ex, params object[] args)
    {
        // Skip if logging is disabled
        if (!_options.Enabled)
            return;

        // Skip if below minimum level
        if (level < _options.MinimumLevel)
            return;

        // Format message
        var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;

        // Build log entry
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelString = level.ToString().ToUpperInvariant();
        var correlationPart = !string.IsNullOrEmpty(_correlationId) ? $"[{_correlationId}]" : "[NO-CORRELATION-ID]";

        var logEntry = $"[{levelString}] [{timestamp}] {correlationPart} {formattedMessage}";

        // Add exception if present
        if (ex != null)
        {
            logEntry += $"\nException: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
        }

        // Write to console
        Console.WriteLine(logEntry);
    }
}
