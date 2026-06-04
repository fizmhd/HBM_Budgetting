using System.Diagnostics;
using BudgetTracker.Web.Logging;

namespace BudgetTracker.Web.Services;

/// <summary>
/// DelegatingHandler that logs HTTP requests and responses.
/// </summary>
public class LoggingHttpHandler : DelegatingHandler
{
    private readonly IClientLogger _logger;
    private readonly CorrelationIdService _correlationIdService;

    public LoggingHttpHandler(IClientLogger logger, CorrelationIdService correlationIdService)
    {
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Add correlation ID to request headers
        var correlationId = _correlationIdService.CorrelationId;
        request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);

        // Update logger's correlation ID
        _logger.SetCorrelationId(correlationId);

        // Log request
        _logger.Debug("HTTP {0} {1}", request.Method, request.RequestUri?.ToString() ?? "null");

        HttpResponseMessage? response = null;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            // Log response based on status code
            if (response.IsSuccessStatusCode)
            {
                _logger.Debug(
                    "HTTP {0} {1} responded {2} in {3}ms",
                    request.Method,
                    request.RequestUri?.ToString() ?? "null",
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.Error(
                    "HTTP {0} {1} responded {2} in {3}ms",
                    null,
                    request.Method,
                    request.RequestUri?.ToString() ?? "null",
                    (int)response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(
                "HTTP {0} {1} failed after {2}ms",
                ex,
                request.Method,
                request.RequestUri?.ToString() ?? "null",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
