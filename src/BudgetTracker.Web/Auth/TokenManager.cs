using Blazored.LocalStorage;

namespace BudgetTracker.Web.Auth;

public class TokenManager
{
    private readonly ILocalStorageService _localStorage;
    private const string AccessTokenKey = "authToken";
    private const string ExpiryKey = "authTokenExpiry";

    public TokenManager(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>(AccessTokenKey);
    }

    public async Task SetTokenAsync(string token, DateTime expiresAt)
    {
        await _localStorage.SetItemAsync(AccessTokenKey, token);
        await _localStorage.SetItemAsync(ExpiryKey, expiresAt);
    }

    public async Task ClearTokenAsync()
    {
        await _localStorage.RemoveItemAsync(AccessTokenKey);
        await _localStorage.RemoveItemAsync(ExpiryKey);
    }

    public async Task<bool> IsTokenValidAsync()
    {
        var token = await _localStorage.GetItemAsync<string>(AccessTokenKey);
        var expiry = await _localStorage.GetItemAsync<DateTime?>(ExpiryKey);

        if (string.IsNullOrEmpty(token) || !expiry.HasValue)
        {
            return false;
        }

        // Add 1 minute buffer to refresh before actual expiry
        return DateTime.UtcNow < expiry.Value.AddMinutes(-1);
    }
}
