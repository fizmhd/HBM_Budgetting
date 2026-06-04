using BudgetTracker.Api.Services.Interfaces;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Auth.ConfirmEmail;

/// <summary>
/// Email confirmation endpoint
/// </summary>
public class ConfirmEmailEndpoint : EndpointWithoutRequest
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;
    private readonly string _frontendUrl;

    public ConfirmEmailEndpoint(
        IAuthService authService,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
        _frontendUrl = configuration["FrontendUrl"] ?? "https://localhost:5001";
    }

    public override void Configure()
    {
        Get("/api/v1/auth/confirm");
        AllowAnonymous();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 5, durationSeconds: 900); // 5 requests per 15 minutes
        }
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Get optional token from query string
        var token = Query<string?>("token", isRequired: false);

        if (string.IsNullOrEmpty(token))
        {
            await SendRedirectAsync($"{_frontendUrl}/auth/login?error=invalid_token", allowRemoteRedirects: true);
            return;
        }

        var result = await _authService.ConfirmEmailAsync(token, ct);

        if (result.IsFailure)
        {
            await SendRedirectAsync($"{_frontendUrl}/auth/login?error=confirmation_failed", allowRemoteRedirects: true);
            return;
        }

        await SendRedirectAsync($"{_frontendUrl}/auth/login?confirmed=true", allowRemoteRedirects: true);
    }
}
