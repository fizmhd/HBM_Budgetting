using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Payslips;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Payslips.GetPayslip;

/// <summary>
/// Returns a single payslip (with line items, summary, YTD and reconciliation) if visible to the caller.
/// </summary>
public class GetPayslipEndpoint : EndpointWithoutRequest<PayslipDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPayslipRepository _payslips;
    private readonly IHouseholdMemberRepository _members;
    private readonly PayslipDtoFactory _dtoFactory;
    private readonly IWebHostEnvironment _environment;

    public GetPayslipEndpoint(
        ICurrentUserService currentUser,
        IPayslipRepository payslips,
        IHouseholdMemberRepository members,
        PayslipDtoFactory dtoFactory,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _payslips = payslips;
        _members = members;
        _dtoFactory = dtoFactory;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/payslips/{id}");

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

        var payslip = await _payslips.GetWithDetailsAsync(Route<Guid>("id"), ct);
        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (payslip is null || !payslip.IsVisibleTo(userId.Value, membership?.HouseholdId))
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var dto = await _dtoFactory.BuildDetailAsync(payslip, userId.Value, membership?.HouseholdId, ct);
        await SendOkAsync(dto, ct);
    }
}
