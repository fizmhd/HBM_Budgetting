using BudgetTracker.Api.Infrastructure.Http;
using BudgetTracker.Api.Infrastructure.Security;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Api.Infrastructure.Options;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace BudgetTracker.Api.Features.Auth.RefreshToken;

/// <summary>
/// Token refresh endpoint
/// </summary>
public class RefreshTokenEndpoint : EndpointWithoutRequest<AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly ICookieService _cookieService;
    private readonly IWebHostEnvironment _environment;

    public RefreshTokenEndpoint(
        IAuthService authService,
        ICookieService cookieService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _cookieService = cookieService;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/auth/refresh");
        AllowAnonymous();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 10, durationSeconds: 900); // 10 requests per 15 minutes
        }
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Get refresh token from cookie
        var refreshToken = _cookieService.GetRefreshToken();

        if (string.IsNullOrEmpty(refreshToken))
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var result = await _authService.RefreshTokenAsync(refreshToken, ct);

        if (result.IsFailure)
        {
            // Clear cookies on failure (especially for reuse detection)
            _cookieService.ClearAuthCookies();
            await SendUnauthorizedAsync(ct);
            return;
        }

        var authResponse = result.Value;

        // Use centralized cookie service
        _cookieService.SetAuthCookies(authResponse);

        await SendOkAsync(authResponse, ct);
    }
}
