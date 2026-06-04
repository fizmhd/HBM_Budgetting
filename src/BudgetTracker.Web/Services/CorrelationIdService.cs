namespace BudgetTracker.Web.Services;

/// <summary>
/// Service for generating and managing correlation IDs for request tracking.
/// </summary>
public class CorrelationIdService
{
    private string _correlationId;

    public CorrelationIdService()
    {
        // Generate a new correlation ID on service initialization
        _correlationId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    public string CorrelationId => _correlationId;

    /// <summary>
    /// Generates a new correlation ID.
    /// </summary>
    public void GenerateNew()
    {
        _correlationId = Guid.NewGuid().ToString();
    }
}
