using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Budgets;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Budgets.UpdateBudget;

/// <summary>
/// Updates an existing budget (re-validating the rules) and returns it with live progress.
/// </summary>
public class UpdateBudgetEndpoint : Endpoint<UpdateBudgetRequest, BudgetDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IBudgetRepository _budgets;
    private readonly IHouseholdMemberRepository _members;
    private readonly BudgetWriteService _writer;
    private readonly BudgetProgressService _progress;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public UpdateBudgetEndpoint(
        ICurrentUserService currentUser,
        IBudgetRepository budgets,
        IHouseholdMemberRepository members,
        BudgetWriteService writer,
        BudgetProgressService progress,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _budgets = budgets;
        _members = members;
        _writer = writer;
        _progress = progress;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Put("/api/v1/budgets/{id}");
        Validator<UpdateBudgetRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(UpdateBudgetRequest req, CancellationToken ct)
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

        var result = await _writer.ApplyAsync(budget, req, userId.Value, membership?.HouseholdId, ct);
        if (result.IsFailure)
        {
            ThrowError(result.Errors[0].Message, 400);
            return;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        var computed = await _progress.ComputeAsync(budget, userId.Value, membership?.HouseholdId, ct);
        if (computed.AlertStateChanged)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        await SendOkAsync(computed.Budgets[0], ct);
    }
}
