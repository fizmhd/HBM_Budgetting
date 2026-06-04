using BudgetTracker.Web.Logging;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BudgetTracker.Shared.DTOs.Auth;

namespace BudgetTracker.Web.Auth;

public class AuthHttpHandler : DelegatingHandler
{
    private readonly TokenManager _tokenManager;
    private readonly NavigationManager _navigationManager;
    private readonly IClientLogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private bool _refreshing = false;

    public AuthHttpHandler(
        TokenManager tokenManager,
        NavigationManager navigationManager,
        IClientLogger logger,
        IHttpClientFactory httpClientFactory)
    {
        _tokenManager = tokenManager;
        _navigationManager = navigationManager;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get token from TokenManager
        var token = await _tokenManager.GetAccessTokenAsync();

        // If token is valid, add to request
        if (!string.IsNullOrEmpty(token) && await _tokenManager.IsTokenValidAsync())
        {
            _logger.Debug("Attaching Bearer token to request {0}", request.RequestUri?.ToString() ?? "unknown");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
             _logger.Warning($"Skipping token attachment. Token present: {!string.IsNullOrEmpty(token)}, Valid: {await _tokenManager.IsTokenValidAsync()}");
        }

        // Send request
        var response = await base.SendAsync(request, cancellationToken);

        // Handle 401 Unauthorized
        // Skip refresh logic for Login request itself to avoid infinite loops
        var isLoginRequest = request.RequestUri?.AbsolutePath.Contains("/auth/login", StringComparison.OrdinalIgnoreCase) == true;
        
        if (response.StatusCode == HttpStatusCode.Unauthorized && !_refreshing && !isLoginRequest)
        {
            _logger.Warning("Received 401 Unauthorized, attempting token refresh");

            _refreshing = true;
            try
            {
                var newToken = await RefreshTokenDirectlyAsync(cancellationToken);

                if (!string.IsNullOrEmpty(newToken))
                {
                    _logger.Info("Token refreshed successfully, retrying request");

                    // Retry the request with new token
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    response = await base.SendAsync(request, cancellationToken);
                }
                else
                {
                    _logger.Warning("Token refresh failed, redirecting to login");
                    _navigationManager.NavigateTo("/login", forceLoad: true);
                }
            }
            finally
            {
                _refreshing = false;
            }
        }

        return response;
    }

    private async Task<string?> RefreshTokenDirectlyAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create a simple HttpClient without the handler chain to avoid circular dependency
            var httpClient = _httpClientFactory.CreateClient("RefreshClient");
            
            var response = await httpClient.PostAsync("/api/v1/auth/refresh", null, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var refreshResponse = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>(cancellationToken: cancellationToken);
                if (refreshResponse != null && !string.IsNullOrEmpty(refreshResponse.AccessToken))
                {
                    await _tokenManager.SetTokenAsync(refreshResponse.AccessToken, refreshResponse.ExpiresAt);
                    return refreshResponse.AccessToken;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.Error($"Network error during token refresh: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.Error($"Detailed token refresh timeout: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.Error($"Invalid response format during token refresh: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Token refresh failed: {ex.Message}");
        }

        // If refresh fails, clear token
        await _tokenManager.ClearTokenAsync();
        return null;
    }
}
