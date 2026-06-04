using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.DTOs.Users;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Users.UpdateSettings;

/// <summary>
/// Update user settings endpoint
/// </summary>
public class UpdateSettingsEndpoint : Endpoint<UpdateSettingsRequest, UserDto>
{
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _environment;

    public UpdateSettingsEndpoint(IUserService userService, IWebHostEnvironment environment)
    {
        _userService = userService;
        _environment = environment;
    }

    public override void Configure()
    {
        Put("/api/v1/users/me/settings");

        // Explicitly configure validator
        Validator<UpdateSettingsRequestValidator>();
        // Requires authentication
        
        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 30, durationSeconds: 60); // 30 requests per minute
        }
    }

    public override async Task HandleAsync(UpdateSettingsRequest req, CancellationToken ct)
    {
        var result = await _userService.UpdateSettingsAsync(req, ct);

        if (result.IsFailure)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
