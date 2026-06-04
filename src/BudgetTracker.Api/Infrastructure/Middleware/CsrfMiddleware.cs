using Microsoft.Extensions.Options;
using BudgetTracker.Api.Infrastructure.Http;
using BudgetTracker.Api.Infrastructure.Options;
using BudgetTracker.Api.Infrastructure.Security;

namespace BudgetTracker.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware for CSRF protection
/// Generates CSRF tokens on login and validates them on state-changing requests
/// </summary>
public class CsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CsrfOptions _options;
    private readonly ILogger<CsrfMiddleware> _logger;

    // HTTP methods that require CSRF protection
    private static readonly HashSet<string> ProtectedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "DELETE", "PATCH"
    };

    // Paths excluded from CSRF validation
    private static readonly HashSet<string> ExcludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/v1/auth/login",
        "/api/v1/auth/register",
        "/api/v1/auth/forgot-password",
        "/api/v1/auth/reset-password",
        "/api/v1/auth/refresh"
    };

    public CsrfMiddleware(
        RequestDelegate next,
        IOptions<CsrfOptions> options,
        ILogger<CsrfMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if CSRF is disabled
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        // Resolve scoped service from request scope
        var csrfService = context.RequestServices.GetRequiredService<ICsrfService>();

        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Validate CSRF token for protected methods
        if (ProtectedMethods.Contains(method) && !ExcludedPaths.Contains(path))
        {
            if (!ValidateCsrfToken(context, csrfService))
            {
                _logger.LogWarning("CSRF validation failed for {Method} {Path}", method, path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "CSRF_VALIDATION_FAILED",
                    message = "CSRF token validation failed"
                });
                return;
            }
        }

        await _next(context);
    }

    private bool ValidateCsrfToken(HttpContext context, ICsrfService csrfService)
    {
        // Get token from header
        if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var headerToken))
        {
            _logger.LogDebug("CSRF header {HeaderName} not found", _options.HeaderName);
            return false;
        }

        // Get token from cookie
        if (!context.Request.Cookies.TryGetValue(_options.CookieName, out var cookieToken))
        {
            _logger.LogDebug("CSRF cookie {CookieName} not found", _options.CookieName);
            return false;
        }

        // Validate tokens match
        return csrfService.ValidateToken(headerToken!, cookieToken!);
    }
}
