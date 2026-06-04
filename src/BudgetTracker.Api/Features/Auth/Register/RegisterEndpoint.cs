using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Auth;
using FastEndpoints;
using BudgetTracker.Api.Infrastructure.Http;
using BudgetTracker.Api.Infrastructure.Security;
using Microsoft.Extensions.Options;
using BudgetTracker.Api.Infrastructure.Options;

namespace BudgetTracker.Api.Features.Auth.Register;

/// <summary>
/// User registration endpoint
/// </summary>
public class RegisterEndpoint : Endpoint<RegisterRequest, AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;
    private readonly ICookieService _cookieService;

    public RegisterEndpoint(
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
        Post("/api/v1/auth/register");
        AllowAnonymous();
        
        // Explicitly configure validator
        Validator<RegisterRequestValidator>();
        
        // Only apply throttling in non-test environments
        if (!_environment.IsEnvironment("Testing")) // Added environment check
        {
            Throttle(hitLimit: 3, durationSeconds: 3600); // 3 requests per hour
        }
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(req, ct);

        if (result.IsFailure)
        {
            // Map errors to appropriate HTTP status codes
            var error = result.Errors.First();
            switch (error.Type)
            {
                case Shared.Results.ErrorType.Validation:
                    ThrowError(error.Message, 400);
                    break;
                case Shared.Results.ErrorType.Conflict:
                    ThrowError(error.Message, 409);
                    break;
                default:
                    ThrowError(error.Message, 500);
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
