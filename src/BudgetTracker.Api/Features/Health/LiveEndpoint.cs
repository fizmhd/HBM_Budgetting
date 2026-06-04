using FastEndpoints;

namespace BudgetTracker.Api.Features.Health;

/// <summary>
/// Basic liveness check endpoint
/// </summary>
public class LiveEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendOkAsync(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }, ct);
    }
}
