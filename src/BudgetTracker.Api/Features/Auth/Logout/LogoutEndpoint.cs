using BudgetTracker.Api.Infrastructure.Http;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Api.Infrastructure.Options;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace BudgetTracker.Api.Features.Auth.Logout;

/// <summary>
/// User logout endpoint
/// </summary>
public class LogoutEndpoint : EndpointWithoutRequest
{
    private readonly IAuthService _authService;
    private readonly ICookieService _cookieService;
    private readonly IWebHostEnvironment _environment;

    public LogoutEndpoint(IAuthService authService, ICookieService cookieService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _cookieService = cookieService;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/auth/logout");
        AllowAnonymous(); // Allow anonymous so users can logout even with expired tokens

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 5, durationSeconds: 900); // 5 requests per 15 minutes
        }
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Get refresh token from cookie
        var refreshToken = _cookieService.GetRefreshToken();

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken, ct);
        }

        // Clear authentication cookies
        _cookieService.ClearAuthCookies();

        await SendNoContentAsync(ct);
    }
}
