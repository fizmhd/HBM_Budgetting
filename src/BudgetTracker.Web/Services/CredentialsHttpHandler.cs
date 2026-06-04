using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace BudgetTracker.Web.Services;

using BudgetTracker.Web.Logging;

public class CredentialsHttpHandler : DelegatingHandler
{
    private readonly IClientLogger _logger;

    public CredentialsHttpHandler(IClientLogger logger)
    {
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        _logger.Debug($"Including credentials for request {request.RequestUri}");
        return base.SendAsync(request, cancellationToken);
    }
}
