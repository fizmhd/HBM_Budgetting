using BudgetTracker.Shared.DTOs.Auth;

namespace BudgetTracker.Api.Infrastructure.Http;

/// <summary>
/// Service for managing authentication cookies
/// </summary>
public interface ICookieService
{
    /// <summary>
    /// Sets authentication cookies (Refresh Token and CSRF) and clears sensitive data from response
    /// </summary>
    void SetAuthCookies(AuthResponse authResponse);

    /// <summary>
    /// Gets the refresh token from the request cookie
    /// </summary>
    string? GetRefreshToken();

    /// <summary>
    /// Clears all authentication cookies
    /// </summary>
    void ClearAuthCookies();
}
