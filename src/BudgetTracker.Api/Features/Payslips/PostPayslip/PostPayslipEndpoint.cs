using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Payslips;
using BudgetTracker.Shared.Results;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Payslips.PostPayslip;

/// <summary>
/// Posts a payslip's net pay as an income transaction on the chosen account (TASK 8.4).
/// </summary>
public class PostPayslipEndpoint : Endpoint<PostPayslipRequest, PostPayslipResultDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPayslipRepository _payslips;
    private readonly IHouseholdMemberRepository _members;
    private readonly Features.Payslips.PayslipPostingService _posting;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public PostPayslipEndpoint(
        ICurrentUserService currentUser,
        IPayslipRepository payslips,
        IHouseholdMemberRepository members,
        Features.Payslips.PayslipPostingService posting,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _payslips = payslips;
        _members = members;
        _posting = posting;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/payslips/{id}/post");
        Validator<PostPayslipRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(PostPayslipRequest req, CancellationToken ct)
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

        var result = await _posting.PostAsync(payslip, req, userId.Value, membership?.HouseholdId, ct);
        if (result.IsFailure)
        {
            var error = result.Errors[0];
            ThrowError(error.Message, error.Type == ErrorType.Conflict ? 409 : 400);
            return;
        }

        _payslips.Update(payslip);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(new PostPayslipResultDto
        {
            PayslipId = payslip.Id,
            TransactionId = result.Value,
            Amount = payslip.DeclaredNet,
            Status = payslip.Status.ToString()
        }, ct);
    }
}
