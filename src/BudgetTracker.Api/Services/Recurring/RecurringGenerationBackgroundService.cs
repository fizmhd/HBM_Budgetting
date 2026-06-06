using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services.Recurring;

/// <summary>
/// Runs the recurring generation engine on a daily timer (TASK 5.2). Resolves the scoped engine in a
/// fresh DI scope per run. Generation is idempotent, so an extra run (e.g. on restart) is harmless.
/// Not registered in the Testing environment, where tests drive generation via the manual endpoint.
/// </summary>
public sealed class RecurringGenerationBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringGenerationBackgroundService> _logger;

    public RecurringGenerationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurringGenerationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A small initial delay lets the app finish starting (and migrations apply) before the first run.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var engine = scope.ServiceProvider.GetRequiredService<IRecurringGenerationService>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var generated = await engine.GenerateDueAsync(today, ownerFilter: null, stoppingToken);
            if (generated > 0)
            {
                _logger.LogInformation("Recurring generation created {Count} occurrence(s).", generated);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Never let a generation failure crash the host; try again next tick.
            _logger.LogError(ex, "Recurring generation run failed.");
        }
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken token)
    {
        try
        {
            return await timer.WaitForNextTickAsync(token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
