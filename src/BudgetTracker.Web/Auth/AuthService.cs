using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Web.Services;
using BudgetTracker.Web.Logging;

namespace BudgetTracker.Web.Auth;

public class AuthService : IAuthService
{
    private readonly IApiClient _apiClient;
    private readonly TokenManager _tokenManager;
    private readonly IClientLogger _logger;
    private UserDto? _cachedUser;

    public AuthService(
        IApiClient apiClient,
        TokenManager tokenManager,
        IClientLogger logger)
    {
        _apiClient = apiClient;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _apiClient.LoginAsync(request);
            if (response != null)
            {
                _logger.Debug($"Login Response Received. AccessToken Present: {!string.IsNullOrEmpty(response.AccessToken)}, Length: {response.AccessToken?.Length ?? 0}");
                if (!string.IsNullOrEmpty(response.AccessToken))
                {
                    await _tokenManager.SetTokenAsync(response.AccessToken, response.ExpiresAt);
                    _cachedUser = response.User;
                }
                else
                {
                    _logger.Debug("Login Response has empty AccessToken!");
                }
            }
            else
            {
                _logger.Debug("Login Response is NULL");
            }
            return response;
        }

        catch (Exception ex)
        {
            _logger.Error("Login failed in AuthService", ex);
            throw;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _apiClient.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.Error("Logout failed", ex);
            // Ignore errors during logout
        }
        finally
        {
            await _tokenManager.ClearTokenAsync();
            _cachedUser = null;
        }
    }

    public async Task<string?> RefreshTokenAsync()
    {
        try
        {
            var response = await _apiClient.RefreshTokenAsync();
            if (response != null && !string.IsNullOrEmpty(response.AccessToken))
            {
                await _tokenManager.SetTokenAsync(response.AccessToken, response.ExpiresAt);
                return response.AccessToken;
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Token refresh failed", ex);
            // If refresh fails, clear token
            await _tokenManager.ClearTokenAsync();
            _cachedUser = null;
        }

        return null;
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        if (_cachedUser != null)
        {
            return _cachedUser;
        }

        if (!await IsAuthenticatedAsync())
        {
            return null;
        }

        try
        {
            _cachedUser = await _apiClient.GetProfileAsync();
            return _cachedUser;
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Valid "user is not logged in" states
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to get current user profile", ex);
            // Rethrow so the UI knows something is wrong (e.g. network error)
            // instead of assuming the user is just logged out
            throw;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        return await _tokenManager.IsTokenValidAsync();
    }
}
