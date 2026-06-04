namespace BudgetTracker.Web.Logging;

/// <summary>
/// Configuration options for client-side logging.
/// </summary>
public class ClientLoggerOptions
{
    /// <summary>
    /// Gets or sets whether logging is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum log level to output.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}
