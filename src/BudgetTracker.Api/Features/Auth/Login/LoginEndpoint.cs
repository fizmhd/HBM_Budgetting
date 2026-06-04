using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Auth;
using FastEndpoints;
using BudgetTracker.Api.Infrastructure.Http;
using BudgetTracker.Api.Infrastructure.Security;

namespace BudgetTracker.Api.Features.Auth.Login;

/// <summary>
/// Login endpoint
/// </summary>
public class LoginEndpoint : Endpoint<LoginRequest, AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;
    private readonly ICookieService _cookieService;

    public LoginEndpoint(
        IAuthService authService,
        IWebHostEnvironment environment,
        ICookieService cookieService)
    {
        _authService = authService;
        _environment = environment;
        _cookieService = cookieService;
    }

    public override void Configure()
    {
        Post("/api/v1/auth/login");
        AllowAnonymous();
        
        // Explicitly configure validator
        Validator<LoginRequestValidator>();
        
        // Only apply throttling in non-test environments
        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 5, durationSeconds: 900); // 5 requests per 15 minutes
        }
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(req, ct);

        if (result.IsFailure)
        {
            var error = result.Errors.First();
            switch (error.Type)
            {
                case Shared.Results.ErrorType.Unauthorized:
                    await SendUnauthorizedAsync(ct);
                    break;
                case Shared.Results.ErrorType.Validation:
                    await SendAsync(new AuthResponse(), 400, ct);
                    break;
                default:
                    await SendAsync(new AuthResponse(), 500, ct);
                    break;
            }
            return;
        }

        var authResponse = result.Value;

        // Use centralized cookie service
        _cookieService.SetAuthCookies(authResponse);

        await SendOkAsync(authResponse, ct);
    }
}
