namespace BudgetTracker.Shared.Results;

/// <summary>
/// Represents a non-generic result indicating success or failure.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets a value indicating whether the result is successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the list of errors. Only populated if IsFailure is true.
    /// </summary>
    public List<Error> Errors { get; }

    protected Result(bool isSuccess, List<Error> errors)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new InvalidOperationException("A successful result cannot have errors.");
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new InvalidOperationException("A failed result must have at least one error.");
        }

        IsSuccess = isSuccess;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, new List<Error>());

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    public static Result Failure(Error error) => new(false, new List<Error> { error });

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    public static Result Failure(List<Error> errors) => new(false, errors);
}
