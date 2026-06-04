using BudgetTracker.Api.Infrastructure.Persistence;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Health;

/// <summary>
/// Readiness check endpoint with dependency health checks
/// </summary>
public class ReadyEndpoint : EndpointWithoutRequest<ReadyResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<ReadyEndpoint> _logger;

    public ReadyEndpoint(
        AppDbContext dbContext,
        Supabase.Client supabaseClient,
        ILogger<ReadyEndpoint> logger)
    {
        _dbContext = dbContext;
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/health/ready");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = new ReadyResponse
        {
            Timestamp = DateTime.UtcNow
        };

        // Check database connectivity
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(ct);
            response.Database = new HealthCheckResult
            {
                Status = canConnect ? "Healthy" : "Unhealthy",
                Message = canConnect ? "Database connection successful" : "Database connection failed"
            };

            if (!canConnect)
            {
                response.OverallStatus = "Unhealthy";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            response.Database = new HealthCheckResult
            {
                Status = "Unhealthy",
                Message = $"Database connection failed: {ex.Message}"
            };
            response.OverallStatus = "Unhealthy";
        }

        // Check Supabase connectivity
        try
        {
            // Simple health check - verify client is initialized
            var isInitialized = _supabaseClient != null;
            response.Supabase = new HealthCheckResult
            {
                Status = isInitialized ? "Healthy" : "Unhealthy",
                Message = isInitialized ? "Supabase client initialized" : "Supabase client not initialized"
            };

            if (!isInitialized)
            {
                response.OverallStatus = "Unhealthy";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Supabase health check failed");
            response.Supabase = new HealthCheckResult
            {
                Status = "Unhealthy",
                Message = $"Supabase check failed: {ex.Message}"
            };
            response.OverallStatus = "Unhealthy";
        }

        var statusCode = response.OverallStatus == "Healthy" ? 200 : 503;
        await SendAsync(response, statusCode, ct);
    }
}

public class ReadyResponse
{
    public string OverallStatus { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; }
    public HealthCheckResult Database { get; set; } = null!;
    public HealthCheckResult Supabase { get; set; } = null!;
}

public class HealthCheckResult
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
