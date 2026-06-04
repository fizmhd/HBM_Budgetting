using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using BudgetTracker.Web.Logging;
using Refit;

namespace BudgetTracker.Web.Services.ErrorHandling;

public class ApiErrorHandler
{
    private readonly IClientLogger _logger;

    public ApiErrorHandler(IClientLogger logger)
    {
        _logger = logger;
    }

    public ErrorResult Handle(Exception ex, string? context = null)
    {
        // Log the full exception with context
        _logger.Error(context ?? "An error occurred", ex);

        return ex switch
        {
            ApiException apiEx => HandleApiException(apiEx),
            HttpRequestException _ => ErrorResult.Create(ErrorMessages.Network.ConnectionError, isRetryable: true),
            TaskCanceledException _ => ErrorResult.Create(ErrorMessages.Network.Timeout, isRetryable: true),
            JsonException _ => ErrorResult.Create(ErrorMessages.Common.GeneralError, technicalDetails: "Response parsing failed"),
            _ => ErrorResult.Create(ErrorMessages.Common.GeneralError)
        };
    }

    private ErrorResult HandleApiException(ApiException ex)
    {
        var statusCode = (int)ex.StatusCode;
        IDictionary<string, string[]>? validationErrors = null;
        string? technicalDetails = ex.Content;
        
        // Try to extract useful info from content
        var message = ExtractErrorMessage(ex, out validationErrors);

        // Fallback to defaults if no message extracted
        if (string.IsNullOrEmpty(message))
        {
            message = ex.StatusCode switch
            {
                HttpStatusCode.BadRequest => ErrorMessages.Validation.Default,
                HttpStatusCode.Unauthorized => ErrorMessages.Auth.SessionExpired,
                HttpStatusCode.Forbidden => ErrorMessages.Auth.Unauthorized,
                HttpStatusCode.NotFound => ErrorMessages.Common.NotFound,
                HttpStatusCode.Conflict => ErrorMessages.Validation.EmailExists,
                HttpStatusCode.UnprocessableEntity => ErrorMessages.Validation.InvalidData,
                HttpStatusCode.TooManyRequests => ErrorMessages.Common.TooManyRequests,
                HttpStatusCode.InternalServerError => ErrorMessages.Network.ServerError,
                _ => ErrorMessages.Common.GeneralError
            };
        }

        return ErrorResult.Create(message, 
            isRetryable: statusCode >= 500 || statusCode == 429, 
            statusCode: statusCode,
            technicalDetails: technicalDetails,
            validationErrors: validationErrors);
    }

    private string? ExtractErrorMessage(ApiException ex, out IDictionary<string, string[]>? validationErrors)
    {
        validationErrors = null;
        try
        {
            if (string.IsNullOrWhiteSpace(ex.Content)) return null;
            
            // Try to parse as JSON
            var jsonNode = JsonNode.Parse(ex.Content);
            if (jsonNode == null) return null;

            // 1. Try "errors" object (Standard Validation Problem Details)
            // { "errors": { "Field": ["Error"] }, "title": "One or more..." }
            if (jsonNode["errors"] is JsonObject errorsNode)
            {
                validationErrors = new Dictionary<string, string[]>();
                foreach (var property in errorsNode)
                {
                   if (property.Value is JsonArray array)
                   {
                       validationErrors[property.Key] = array.Select(x => x?.ToString() ?? "").ToArray();
                   }
                   else if (property.Value != null)
                   {
                       validationErrors[property.Key] = new[] { property.Value.ToString() };
                   }
                }
                
                // If we found validation errors, return a standard message
                if (validationErrors.Any()) return ErrorMessages.Validation.InvalidData;
            }

            // 2. Try "detail" property (Standard ProblemDetails)
            if (jsonNode["detail"] != null)
            {
                return jsonNode["detail"]?.ToString();
            }

            // 3. Try "message" property (Simple error object)
            // { "message": "Custom error" }
            if (jsonNode["message"] != null)
            {
                return jsonNode["message"]?.ToString();
            }
             
            return null;
        }
        catch
        {
            // If parsing fails, just return null and let generic handlers take over
            return null;
        }
    }
}
