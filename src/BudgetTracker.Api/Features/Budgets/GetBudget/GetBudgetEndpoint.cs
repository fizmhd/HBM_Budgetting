using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Budgets;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Budgets.GetBudget;

/// <summary>
/// Returns a single budget with live progress if visible to the caller (TASK 6.4).
/// </summary>
public class GetBudgetEndpoint : EndpointWithoutRequest<BudgetDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IBudgetRepository _budgets;
    private readonly IHouseholdMemberRepository _members;
    private readonly BudgetProgressService _progress;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public GetBudgetEndpoint(
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
        Get("/api/v1/budgets/{id}");

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

        var budget = await _budgets.GetByIdAsync(Route<Guid>("id"), ct);
        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (budget is null || !budget.IsVisibleTo(userId.Value, membership?.HouseholdId))
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var computed = await _progress.ComputeAsync(budget, userId.Value, membership?.HouseholdId, ct);
        if (computed.AlertStateChanged)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        await SendOkAsync(computed.Budgets[0], ct);
    }
}
