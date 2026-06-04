using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Auth;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Auth.ForgotPassword;

/// <summary>
/// Forgot password endpoint
/// </summary>
public class ForgotPasswordEndpoint : Endpoint<ForgotPasswordRequest>
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public ForgotPasswordEndpoint(IAuthService authService, IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/auth/forgot-password");
        AllowAnonymous();
        
        // Explicitly configure validator
        Validator<ForgotPasswordRequestValidator>();
        
        // Only apply throttling in non-test environments
        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 3, durationSeconds: 3600); // 3 requests per hour
        }
    }

    public override async Task HandleAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        // Call the service (always returns success for security)
        await _authService.ForgotPasswordAsync(req, ct);

        // Always return 200 OK with no body (don't reveal if email exists - security best practice)
        await SendOkAsync(ct);
    }
}
