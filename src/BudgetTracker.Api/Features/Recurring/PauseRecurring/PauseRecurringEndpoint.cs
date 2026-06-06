using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Recurring;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Recurring.PauseRecurring;

/// <summary>
/// Pauses a recurring rule so the generation engine skips it until resumed (TASK 5.3).
/// </summary>
public class PauseRecurringEndpoint : EndpointWithoutRequest<RecurringRuleDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecurringRuleRepository _rules;
    private readonly IHouseholdMemberRepository _members;
    private readonly RecurringDtoFactory _dtoFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public PauseRecurringEndpoint(
        ICurrentUserService currentUser,
        IRecurringRuleRepository rules,
        IHouseholdMemberRepository members,
        RecurringDtoFactory dtoFactory,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _rules = rules;
        _members = members;
        _dtoFactory = dtoFactory;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/recurring/{id}/pause");

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

        var rule = await _rules.GetByIdAsync(Route<Guid>("id"), ct);
        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (rule is null || !rule.IsVisibleTo(userId.Value, membership?.HouseholdId))
        {
            await SendNotFoundAsync(ct);
            return;
        }

        if (rule.Status != RecurringStatus.Paused)
        {
            rule.Status = RecurringStatus.Paused;
            rule.PausedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var mapper = await _dtoFactory.CreateMapperAsync(userId.Value, membership?.HouseholdId, ct);
        await SendOkAsync(mapper(rule), ct);
    }
}
