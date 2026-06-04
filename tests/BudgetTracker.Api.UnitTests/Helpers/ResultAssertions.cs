using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.UnitTests.Helpers;

public class ResultAssertions<T> : ReferenceTypeAssertions<Result<T>, ResultAssertions<T>>
{
    public ResultAssertions(Result<T> subject)
        : base(subject)
    {
    }

    protected override string Identifier => "Result";

    public AndConstraint<ResultAssertions<T>> BeSuccess(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsSuccess)
            .FailWith("Expected {context:Result} to be successful{reason}, but it failed with errors: {0}", 
                string.Join(", ", Subject.Errors.Select(e => $"{e.Code}: {e.Message}")));

        return new AndConstraint<ResultAssertions<T>>(this);
    }

    public AndConstraint<ResultAssertions<T>> BeFailure(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!Subject.IsSuccess)
            .FailWith("Expected {context:Result} to be failure{reason}, but it succeeded.");

        return new AndConstraint<ResultAssertions<T>>(this);
    }
    
    public AndConstraint<ResultAssertions<T>> HaveError(string expectedErrorCode, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!Subject.IsSuccess)
            .FailWith("Expected {context:Result} to be failure{reason}, but it succeeded.")
            .Then
            .ForCondition(Subject.Errors.Any(e => e.Code == expectedErrorCode))
            .FailWith("Expected {context:Result} to have error with code {0}{reason}, but found {1}.", expectedErrorCode, Subject.Errors);

        return new AndConstraint<ResultAssertions<T>>(this);
    }
}

public static class ResultExtensions
{
    public static ResultAssertions<T> Should<T>(this Result<T> instance)
    {
        return new ResultAssertions<T>(instance);
    }
}
