namespace BudgetTracker.Web.Logging;

/// <summary>
/// Interface for client-side logging.
/// </summary>
public interface IClientLogger
{
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    void Debug(string message, params object[] args);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    void Info(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    void Warning(string message, params object[] args);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    void Error(string message, Exception? ex = null, params object[] args);

    /// <summary>
    /// Sets the current correlation ID for subsequent log entries.
    /// </summary>
    void SetCorrelationId(string? correlationId);
}
