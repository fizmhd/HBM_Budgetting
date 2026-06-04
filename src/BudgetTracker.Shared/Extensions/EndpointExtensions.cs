using BudgetTracker.Shared.Results;

namespace BudgetTracker.Shared.Extensions;

/// <summary>
/// Extension methods for mapping Result to HTTP responses in FastEndpoints.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Maps a Result to an HTTP status code based on the error type.
    /// </summary>
    public static int ToStatusCode(this Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => 400,      // Bad Request
            ErrorType.NotFound => 404,        // Not Found
            ErrorType.Unauthorized => 401,    // Unauthorized
            ErrorType.Conflict => 409,        // Conflict
            ErrorType.Internal => 500,        // Internal Server Error
            _ => 500
        };
    }

    /// <summary>
    /// Maps a Result to an HTTP status code. Returns 200 for success, or the appropriate error code.
    /// </summary>
    public static int ToStatusCode(this Result result)
    {
        if (result.IsSuccess)
        {
            return 200; // OK
        }

        // Return the status code of the first error
        return result.Errors.FirstOrDefault()?.ToStatusCode() ?? 500;
    }

    /// <summary>
    /// Maps a Result{T} to an HTTP status code. Returns 200 for success, or the appropriate error code.
    /// </summary>
    public static int ToStatusCode<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return 200; // OK
        }

        // Return the status code of the first error
        return result.Errors.FirstOrDefault()?.ToStatusCode() ?? 500;
    }

    /// <summary>
    /// Determines if the result should return 201 Created instead of 200 OK.
    /// </summary>
    public static int ToCreatedStatusCode<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return 201; // Created
        }

        return result.Errors.FirstOrDefault()?.ToStatusCode() ?? 500;
    }
}
