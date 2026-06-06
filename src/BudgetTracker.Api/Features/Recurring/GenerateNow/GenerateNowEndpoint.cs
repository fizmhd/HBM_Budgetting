using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Recurring;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Recurring.GenerateNow;

/// <summary>
/// Manually runs the generation engine for the caller's own rules (TASK 5.2 — testing/on-demand). The
/// scheduled background job covers all owners automatically.
/// </summary>
public class GenerateNowEndpoint : EndpointWithoutRequest<RecurringGenerationResultDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecurringGenerationService _generation;
    private readonly IWebHostEnvironment _environment;

    public GenerateNowEndpoint(
        ICurrentUserService currentUser,
        IRecurringGenerationService generation,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _generation = generation;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/recurring/generate");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 30, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var generated = await _generation.GenerateDueAsync(today, ownerFilter: userId.Value, ct);

        await SendOkAsync(new RecurringGenerationResultDto { Generated = generated }, ct);
    }
}
