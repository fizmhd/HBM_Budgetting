using BudgetTracker.Web.Logging;

namespace BudgetTracker.Web.Services;

public class CorrelationIdHandler : DelegatingHandler
{
    private readonly CorrelationIdService _correlationIdService;
    private readonly IClientLogger _logger;

    public CorrelationIdHandler(CorrelationIdService correlationIdService, IClientLogger logger)
    {
        _correlationIdService = correlationIdService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = _correlationIdService.CorrelationId;
        request.Headers.Add("X-Correlation-ID", correlationId);

        _logger.Debug($"Added correlation ID {correlationId} to request {request.Method} {request.RequestUri}");

        return await base.SendAsync(request, cancellationToken);
    }
}
