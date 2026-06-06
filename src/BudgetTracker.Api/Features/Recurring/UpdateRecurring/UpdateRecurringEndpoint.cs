using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Recurring;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Recurring.UpdateRecurring;

/// <summary>
/// Updates an existing recurring rule (re-validating the rules and recomputing next-due).
/// </summary>
public class UpdateRecurringEndpoint : Endpoint<UpdateRecurringRuleRequest, RecurringRuleDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecurringRuleRepository _rules;
    private readonly IHouseholdMemberRepository _members;
    private readonly RecurringWriteService _writer;
    private readonly RecurringDtoFactory _dtoFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public UpdateRecurringEndpoint(
        ICurrentUserService currentUser,
        IRecurringRuleRepository rules,
        IHouseholdMemberRepository members,
        RecurringWriteService writer,
        RecurringDtoFactory dtoFactory,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _rules = rules;
        _members = members;
        _writer = writer;
        _dtoFactory = dtoFactory;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Put("/api/v1/recurring/{id}");
        Validator<UpdateRecurringRuleRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(UpdateRecurringRuleRequest req, CancellationToken ct)
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

        var result = await _writer.ApplyAsync(rule, req, userId.Value, membership?.HouseholdId, ct);
        if (result.IsFailure)
        {
            ThrowError(result.Errors[0].Message, 400);
            return;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        var mapper = await _dtoFactory.CreateMapperAsync(userId.Value, membership?.HouseholdId, ct);
        await SendOkAsync(mapper(rule), ct);
    }
}
