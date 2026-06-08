using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Payslips.DeletePayslip;

/// <summary>
/// Deletes a payslip (and its line items / leave balances, by cascade) if visible to the caller. An
/// already-posted payslip's income transaction is a real money record and is left untouched.
/// </summary>
public class DeletePayslipEndpoint : EndpointWithoutRequest
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPayslipRepository _payslips;
    private readonly IHouseholdMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public DeletePayslipEndpoint(
        ICurrentUserService currentUser,
        IPayslipRepository payslips,
        IHouseholdMemberRepository members,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _payslips = payslips;
        _members = members;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Delete("/api/v1/payslips/{id}");

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

        var payslip = await _payslips.GetByIdAsync(Route<Guid>("id"), ct);
        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (payslip is null || !payslip.IsVisibleTo(userId.Value, membership?.HouseholdId))
        {
            await SendNotFoundAsync(ct);
            return;
        }

        _payslips.Delete(payslip);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
