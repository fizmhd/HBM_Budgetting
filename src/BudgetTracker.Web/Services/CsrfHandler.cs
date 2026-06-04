using BudgetTracker.Web.Logging;
using Microsoft.JSInterop;
using Microsoft.Extensions.Options;

namespace BudgetTracker.Web.Services;

public class CsrfHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IClientLogger _logger;
    private readonly CsrfOptions _options;

    public CsrfHandler(IJSRuntime jsRuntime, IClientLogger logger, IOptions<CsrfOptions> options)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Only add CSRF token for state-changing methods
        if (request.Method == HttpMethod.Post ||
            request.Method == HttpMethod.Put ||
            request.Method == HttpMethod.Delete ||
            request.Method == HttpMethod.Patch)
        {
            try
            {
                // Get CSRF token from storage using safe helper
                var csrfToken = await _jsRuntime.InvokeAsync<string>(
                    "BudgetTracker.getCsrfToken");

                if (!string.IsNullOrEmpty(csrfToken))
                {
                    // Use configured header name
                    request.Headers.Add(_options.HeaderName, csrfToken);
                    _logger.Debug($"Added CSRF token to request {request.Method} {request.RequestUri}");
                }
                else
                {
                    _logger.Warning($"No CSRF token found for request {request.Method} {request.RequestUri}");
                }
            }
            catch (JSException ex)
            {
                _logger.Warning($"JS Interop error getting CSRF token: {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                // Component disposed, ignore
            }
            catch (InvalidOperationException ex)
            {
                _logger.Warning($"JSRuntime unavailable for CSRF token: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to get CSRF token: {ex.Message}");
            }
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Capture CSRF token from response header if present
        if (response.Headers.TryGetValues(_options.HeaderName, out var tokenValues))
        {
            var token = tokenValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("BudgetTracker.storeCsrfToken", token);
                    _logger.Debug("Captured and stored new CSRF token");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to store CSRF token: {ex.Message}");
                }
            }
        }

        return response;
    }
}
