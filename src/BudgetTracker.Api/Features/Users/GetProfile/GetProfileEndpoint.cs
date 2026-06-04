using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Auth;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Users.GetProfile;

/// <summary>
/// Get current user profile endpoint
/// </summary>
public class GetProfileEndpoint : EndpointWithoutRequest<UserDto>
{
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _environment;

    public GetProfileEndpoint(IUserService userService, IWebHostEnvironment environment)
    {
        _userService = userService;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/users/me");
        // Requires authentication - will be enforced by middleware

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 30, durationSeconds: 60); // 30 requests per minute
        }
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _userService.GetProfileAsync(ct);

        if (result.IsFailure)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        await SendOkAsync(result.Value, ct);
    }
}
