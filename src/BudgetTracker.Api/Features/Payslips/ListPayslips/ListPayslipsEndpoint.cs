using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Payslips;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Payslips.ListPayslips;

/// <summary>
/// Lists the payslips visible to the caller, newest pay date first (TASK 8.3). Header only.
/// </summary>
public class ListPayslipsEndpoint : EndpointWithoutRequest<List<PayslipListItemDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPayslipRepository _payslips;
    private readonly IHouseholdMemberRepository _members;
    private readonly IWebHostEnvironment _environment;

    public ListPayslipsEndpoint(
        ICurrentUserService currentUser,
        IPayslipRepository payslips,
        IHouseholdMemberRepository members,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _payslips = payslips;
        _members = members;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/payslips");

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
        var payslips = await _payslips.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct);

        await SendOkAsync(payslips.Select(p => p.ToListItemDto()).ToList(), ct);
    }
}
