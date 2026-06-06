using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Budgets;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Budgets.CreateBudget;

/// <summary>
/// Creates a category budget for the caller and returns it with live progress (TASK 6.4).
/// </summary>
public class CreateBudgetEndpoint : Endpoint<CreateBudgetRequest, BudgetDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IBudgetRepository _budgets;
    private readonly IHouseholdMemberRepository _members;
    private readonly BudgetWriteService _writer;
    private readonly BudgetProgressService _progress;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public CreateBudgetEndpoint(
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
        Post("/api/v1/budgets");
        Validator<CreateBudgetRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CreateBudgetRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);

        var budget = new Budget { Id = Guid.NewGuid() };
        var result = await _writer.ApplyAsync(budget, req, userId.Value, membership?.HouseholdId, ct);
        if (result.IsFailure)
        {
            ThrowError(result.Errors[0].Message, 400);
            return;
        }

        await _budgets.AddAsync(budget, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var computed = await _progress.ComputeAsync(budget, userId.Value, membership?.HouseholdId, ct);
        if (computed.AlertStateChanged)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        await SendOkAsync(computed.Budgets[0], ct);
    }
}
