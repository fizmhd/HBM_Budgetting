using BudgetTracker.Api.Infrastructure.Options;
using BudgetTracker.Api.Infrastructure.Security;
using BudgetTracker.Shared.DTOs.Auth;
using Microsoft.Extensions.Options;

namespace BudgetTracker.Api.Infrastructure.Http;

/// <summary>
/// Implementation of cookie management service
/// </summary>
public class CookieService : ICookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICsrfService _csrfService;
    private readonly AuthOptions _authOptions;
    private readonly CsrfOptions _csrfOptions;

    private const string RefreshTokenCookieName = "refresh_token";
    private const string CookiePath = "/api/v1/auth";

    public CookieService(
        IHttpContextAccessor httpContextAccessor,
        ICsrfService csrfService,
        IOptions<AuthOptions> authOptions,
        IOptions<CsrfOptions> csrfOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _csrfService = csrfService;
        _authOptions = authOptions.Value;
        _csrfOptions = csrfOptions.Value;
    }

    public void SetAuthCookies(AuthResponse authResponse)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        // Set refresh token cookie if present
        if (!string.IsNullOrEmpty(authResponse.RefreshToken))
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = _authOptions.CookieSecure,
                SameSite = ParseSameSiteMode(_authOptions.CookieSameSite),
                Path = CookiePath,
                Expires = DateTimeOffset.UtcNow.AddDays(_authOptions.RefreshTokenExpirationDays)
            };

            context.Response.Cookies.Append(RefreshTokenCookieName, authResponse.RefreshToken, cookieOptions);
            
            // Clear the refresh token from the response so it's not sent in the body
            authResponse.RefreshToken = null;
        }

        // Generate and set CSRF token
        var csrfToken = _csrfService.GenerateToken();
        SetCsrfTokenCookie(context, csrfToken);
    }

    public string? GetRefreshToken()
    {
        return _httpContextAccessor.HttpContext?.Request.Cookies[RefreshTokenCookieName];
    }

    public void ClearAuthCookies()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        context.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            Path = CookiePath
        });

        if (!string.IsNullOrEmpty(_csrfOptions.CookieName))
        {
            context.Response.Cookies.Delete(_csrfOptions.CookieName, new CookieOptions
            {
                Path = "/"
            });
        }
    }

    private void SetCsrfTokenCookie(HttpContext context, string csrfToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = _csrfOptions.CookieHttpOnly,
            Secure = _csrfOptions.CookieSecure,
            SameSite = ParseSameSiteMode(_csrfOptions.CookieSameSite),
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(_csrfOptions.TokenExpirationMinutes)
        };

        context.Response.Cookies.Append(_csrfOptions.CookieName, csrfToken, cookieOptions);
        
        // Also add to header for client-side access (SPA support)
        context.Response.Headers.Append(_csrfOptions.HeaderName, csrfToken);
    }

    private static SameSiteMode ParseSameSiteMode(string sameSite)
    {
        return sameSite.ToLowerInvariant() switch
        {
            "strict" => SameSiteMode.Strict,
            "lax" => SameSiteMode.Lax,
            "none" => SameSiteMode.None,
            _ => SameSiteMode.Strict
        };
    }
}
