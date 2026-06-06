using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Dashboard;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Dashboard.GetMonthlyDashboard;

/// <summary>
/// Returns the at-a-glance monthly dashboard for the caller (TASK 7.1 / R8):
/// <c>GET /api/v1/dashboard/monthly?month=YYYY-MM&amp;scope=individual|household</c>.
/// </summary>
public class GetMonthlyDashboardEndpoint : EndpointWithoutRequest<MonthlyDashboardDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IHouseholdMemberRepository _members;
    private readonly IDashboardService _dashboard;
    private readonly IWebHostEnvironment _environment;

    public GetMonthlyDashboardEndpoint(
        ICurrentUserService currentUser,
        IHouseholdMemberRepository members,
        IDashboardService dashboard,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _members = members;
        _dashboard = dashboard;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/dashboard/monthly");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
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

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);

        var (year, month) = ParseMonth(Query<string>("month", isRequired: false));

        // Default to the household (full visible) picture; "individual" narrows to the caller's own.
        var scope = Query<string>("scope", isRequired: false);
        var householdScope = !string.Equals(scope, "individual", StringComparison.OrdinalIgnoreCase);

        var dto = await _dashboard.BuildMonthlyAsync(
            userId.Value, membership?.HouseholdId, householdScope, year, month, ct);

        await SendOkAsync(dto, ct);
    }

    /// <summary>
    /// Parses a "yyyy-MM" month, falling back to the current UTC month when missing or malformed.
    /// </summary>
    private static (int Year, int Month) ParseMonth(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw) &&
            DateOnly.TryParse($"{raw}-01", out var parsed))
        {
            return (parsed.Year, parsed.Month);
        }

        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        return (now.Year, now.Month);
    }
}
