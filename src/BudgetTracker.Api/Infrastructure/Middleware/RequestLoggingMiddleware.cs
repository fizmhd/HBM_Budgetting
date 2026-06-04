using System.Diagnostics;
using System.Security.Claims;
using BudgetTracker.Api.Infrastructure.Logging;

namespace BudgetTracker.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware that logs HTTP request and response details with structured logging.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationIdContext.CorrelationId;
        var requestId = context.TraceIdentifier;

        // Log request
        _logger.LogInformation(
            "HTTP {Method} {Path}{QueryString} started. CorrelationId: {CorrelationId}, RequestId: {RequestId}, UserId: {UserId}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            correlationId,
            requestId,
            GetUserId(context));

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var logLevel = GetLogLevel(statusCode);

            // Log response
            _logger.Log(
                logLevel,
                "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }

    private static string? GetUserId(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static LogLevel GetLogLevel(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ => LogLevel.Information
        };
    }
}
