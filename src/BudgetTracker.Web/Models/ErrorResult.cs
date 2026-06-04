namespace BudgetTracker.Web.Models;

public class ErrorResult
{
    public bool Success { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public Dictionary<string, string[]> ValidationErrors { get; set; } = new();
    public bool IsRetryable { get; set; }
    public int? StatusCode { get; set; }
    public string? TechnicalDetails { get; set; }

    public static ErrorResult Create(string message, bool isRetryable = false, int? statusCode = null, string? technicalDetails = null, IDictionary<string, string[]>? validationErrors = null)
    {
        return new ErrorResult
        {
            Success = false,
            UserMessage = message,
            IsRetryable = isRetryable,
            StatusCode = statusCode,
            TechnicalDetails = technicalDetails,
            ValidationErrors = validationErrors != null ? new Dictionary<string, string[]>(validationErrors) : new Dictionary<string, string[]>()
        };
    }

    public static ErrorResult Failure(string message) => Create(message);
    
    public static ErrorResult SuccessResult() => new() { Success = true };
}
