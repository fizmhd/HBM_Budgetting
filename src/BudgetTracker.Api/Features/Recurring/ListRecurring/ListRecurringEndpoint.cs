using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Recurring;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Recurring.ListRecurring;

/// <summary>
/// Lists the recurring rules visible to the caller, optionally filtered by kind (TASK 5.4 / 5.5):
/// <c>?kind=expense|income|subscription</c>.
/// </summary>
public class ListRecurringEndpoint : EndpointWithoutRequest<List<RecurringRuleDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IRecurringRuleRepository _rules;
    private readonly IHouseholdMemberRepository _members;
    private readonly RecurringDtoFactory _dtoFactory;
    private readonly IWebHostEnvironment _environment;

    public ListRecurringEndpoint(
        ICurrentUserService currentUser,
        IRecurringRuleRepository rules,
        IHouseholdMemberRepository members,
        RecurringDtoFactory dtoFactory,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _rules = rules;
        _members = members;
        _dtoFactory = dtoFactory;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/recurring");

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
        var rules = await _rules.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct);

        var kind = Query<string>("kind", isRequired: false);
        rules = kind?.ToLowerInvariant() switch
        {
            "income" => rules.Where(r => r.Type == TransactionType.Income).ToList(),
            "expense" => rules.Where(r => r.Type == TransactionType.Expense && !r.IsSubscription).ToList(),
            "subscription" => rules.Where(r => r.IsSubscription).ToList(),
            _ => rules
        };

        var mapper = await _dtoFactory.CreateMapperAsync(userId.Value, membership?.HouseholdId, ct);
        await SendOkAsync(rules.Select(mapper).ToList(), ct);
    }
}
