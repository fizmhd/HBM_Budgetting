namespace BudgetTracker.Api.Infrastructure.Logging;

/// <summary>
/// Provides thread-safe storage for the current correlation ID.
/// </summary>
public static class CorrelationIdContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    /// <summary>
    /// Gets or sets the current correlation ID for this async context.
    /// </summary>
    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }
}
