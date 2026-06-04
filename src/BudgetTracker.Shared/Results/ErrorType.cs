namespace BudgetTracker.Shared.Results;

/// <summary>
/// Defines the types of errors that can occur in the application.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Validation error (e.g., invalid input data).
    /// </summary>
    Validation,

    /// <summary>
    /// Resource not found error.
    /// </summary>
    NotFound,

    /// <summary>
    /// Unauthorized access error.
    /// </summary>
    Unauthorized,

    /// <summary>
    /// Conflict error (e.g., duplicate resource).
    /// </summary>
    Conflict,

    /// <summary>
    /// Internal server error.
    /// </summary>
    Internal
}
