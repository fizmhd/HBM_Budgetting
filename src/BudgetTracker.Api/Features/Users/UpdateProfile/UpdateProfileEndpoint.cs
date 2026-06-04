using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.DTOs.Users;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Users.UpdateProfile;

/// <summary>
/// Update user profile endpoint
/// </summary>
public class UpdateProfileEndpoint : Endpoint<UpdateProfileRequest, UserDto>
{
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _environment;

    public UpdateProfileEndpoint(IUserService userService, IWebHostEnvironment environment)
    {
        _userService = userService;
        _environment = environment;
    }

    public override void Configure()
    {
        Put("/api/v1/users/me");

        // Explicitly configure validator
        Validator<UpdateProfileRequestValidator>();
        // Requires authentication

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 30, durationSeconds: 60); // 30 requests per minute
        }
    }

    public override async Task HandleAsync(UpdateProfileRequest req, CancellationToken ct)
    {
        var result = await _userService.UpdateProfileAsync(req, ct);

        if (result.IsFailure)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
