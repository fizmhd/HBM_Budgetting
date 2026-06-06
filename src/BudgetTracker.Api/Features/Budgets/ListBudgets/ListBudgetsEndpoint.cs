using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Budgets;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Budgets.ListBudgets;

/// <summary>
/// Lists the budgets visible to the caller with live progress (TASK 6.4). Optional <c>from</c>/<c>to</c>
/// query params restrict the result to budgets whose period overlaps that window (the web page passes
/// the selected month).
/// </summary>
public class ListBudgetsEndpoint : EndpointWithoutRequest<List<BudgetDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IBudgetRepository _budgets;
    private readonly IHouseholdMemberRepository _members;
    private readonly BudgetProgressService _progress;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public ListBudgetsEndpoint(
        ICurrentUserService currentUser,
        IBudgetRepository budgets,
        IHouseholdMemberRepository members,
        BudgetProgressService progress,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _budgets = budgets;
        _members = members;
        _progress = progress;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/budgets");

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

        var from = ParseDate(Query<string>("from", isRequired: false));
        var to = ParseDate(Query<string>("to", isRequired: false));

        var budgets = await _budgets.GetVisibleAsync(userId.Value, membership?.HouseholdId, from, to, ct);
        var computed = await _progress.ComputeAsync(budgets, userId.Value, membership?.HouseholdId, ct);
        if (computed.AlertStateChanged)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        await SendOkAsync(computed.Budgets, ct);
    }

    private static DateOnly? ParseDate(string? raw) =>
        DateOnly.TryParse(raw, out var date) ? date : null;
}
