using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.DTOs.Users;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Users.CompleteProfile;

/// <summary>
/// Complete user profile endpoint
/// </summary>
public class CompleteProfileEndpoint : Endpoint<CompleteProfileRequest, UserDto>
{
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _environment;

    public CompleteProfileEndpoint(IUserService userService, IWebHostEnvironment environment)
    {
        _userService = userService;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/users/me/complete-profile");

        // Explicitly configure validator
        Validator<CompleteProfileRequestValidator>();
        // Requires authentication

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 30, durationSeconds: 60); // 30 requests per minute
        }
    }

    public override async Task HandleAsync(CompleteProfileRequest req, CancellationToken ct)
    {
        var result = await _userService.CompleteProfileAsync(req, ct);

        if (result.IsFailure)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
