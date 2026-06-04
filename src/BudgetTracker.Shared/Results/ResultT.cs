namespace BudgetTracker.Shared.Results;

/// <summary>
/// Represents a generic result with a value of type T.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value. Only accessible if IsSuccess is true.
    /// </summary>
    public T Value
    {
        get
        {
            if (IsFailure)
            {
                throw new InvalidOperationException("Cannot access Value on a failed result.");
            }

            return _value!;
        }
    }

    private Result(T value) : base(true, new List<Error>())
    {
        _value = value;
    }

    private Result(List<Error> errors) : base(false, errors)
    {
        _value = default;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with a single error.
    /// </summary>
    public new static Result<T> Failure(Error error) => new(new List<Error> { error });

    /// <summary>
    /// Creates a failed result with multiple errors.
    /// </summary>
    public new static Result<T> Failure(List<Error> errors) => new(errors);

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicitly converts an error to a failed result.
    /// </summary>
    public static implicit operator Result<T>(Error error) => Failure(error);

    /// <summary>
    /// Implicitly converts a list of errors to a failed result.
    /// </summary>
    public static implicit operator Result<T>(List<Error> errors) => Failure(errors);
}
