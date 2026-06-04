using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Auth;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Auth.ResetPassword;

/// <summary>
/// Reset password endpoint
/// </summary>
public class ResetPasswordEndpoint : Endpoint<ResetPasswordRequest>
{
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _environment;

    public ResetPasswordEndpoint(
        IAuthService authService,
        IWebHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/auth/reset-password");
        AllowAnonymous();

        // Explicitly configure validator
        Validator<ResetPasswordRequestValidator>();

        // Only apply throttling in non-test environments
        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 5, durationSeconds: 3600); // 5 requests per hour
        }
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var result = await _authService.ResetPasswordAsync(req, ct);

        if (result.IsFailure)
        {
            var error = result.Errors.First();
            ThrowError(error.Message, 400);
            return;
        }

        // Return 204 No Content on success
        await SendNoContentAsync(ct);
    }
}
