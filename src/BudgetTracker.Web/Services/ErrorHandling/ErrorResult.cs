namespace BudgetTracker.Web.Services.ErrorHandling;

public class ErrorResult
{
    public string UserMessage { get; set; } = string.Empty;
    public string? TechnicalDetails { get; set; }
    public int? StatusCode { get; set; }
    public bool IsRetryable { get; set; }
    public IDictionary<string, string[]>? ValidationErrors { get; set; }

    public static ErrorResult Create(
        string message, 
        bool isRetryable = false, 
        int? statusCode = null, 
        string? technicalDetails = null,
        IDictionary<string, string[]>? validationErrors = null)
    {
        return new ErrorResult
        {
            UserMessage = message,
            IsRetryable = isRetryable,
            StatusCode = statusCode,
            TechnicalDetails = technicalDetails,
            ValidationErrors = validationErrors
        };
    }
}
