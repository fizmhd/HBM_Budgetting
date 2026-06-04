namespace BudgetTracker.Shared.Results;

/// <summary>
/// Extension methods for Result and Result{T}.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a successful result to a new result with a different value type.
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper)
    {
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors);
        }

        return Result<TOut>.Success(mapper(result.Value));
    }

    /// <summary>
    /// Binds a successful result to a function that returns a new result.
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> binder)
    {
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors);
        }

        return binder(result.Value);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<List<Error>> action)
    {
        if (result.IsFailure)
        {
            action(result.Errors);
        }

        return result;
    }

    /// <summary>
    /// Matches the result to one of two functions based on success or failure.
    /// </summary>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<List<Error>, TOut> onFailure)
    {
        return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Errors);
    }

    /// <summary>
    /// Converts a Result to Result{T} with a default value.
    /// </summary>
    public static Result<T> ToResult<T>(this Result result, T value)
    {
        return result.IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(result.Errors);
    }
}
