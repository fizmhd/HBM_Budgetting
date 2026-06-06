using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Recurring;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Recurring.CreateRecurring;

/// <summary>
/// Creates a recurring rule for the caller (TASK 5.4).
/// </summary>
public class CreateRecurringEndpoint : Endpoint<CreateRecurringRuleRequest, RecurringRuleDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecurringRuleRepository _rules;
    private readonly IHouseholdMemberRepository _members;
    private readonly RecurringWriteService _writer;
    private readonly RecurringDtoFactory _dtoFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public CreateRecurringEndpoint(
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
        Post("/api/v1/recurring");
        Validator<CreateRecurringRuleRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CreateRecurringRuleRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);

        var rule = new RecurringRule { Id = Guid.NewGuid() };
        var result = await _writer.ApplyAsync(rule, req, userId.Value, membership?.HouseholdId, ct);
        if (result.IsFailure)
        {
            ThrowError(result.Errors[0].Message, 400);
            return;
        }

        await _rules.AddAsync(rule, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var mapper = await _dtoFactory.CreateMapperAsync(userId.Value, membership?.HouseholdId, ct);
        await SendOkAsync(mapper(rule), ct);
    }
}
