using Serilog.Context;
using BudgetTracker.Api.Infrastructure.Logging;

namespace BudgetTracker.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware that extracts or generates a correlation ID for request tracking.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract correlation ID from header or generate new one
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        // Store in context for access throughout the request
        CorrelationIdContext.CorrelationId = correlationId;

        // Add to Serilog LogContext so it appears in all logs
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Add to response headers
            context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);

            _logger.LogDebug("Correlation ID set: {CorrelationId}", correlationId);

            await _next(context);
        }
    }
}
