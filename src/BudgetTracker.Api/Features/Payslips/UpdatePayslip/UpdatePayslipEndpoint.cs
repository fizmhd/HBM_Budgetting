using BudgetTracker.Api.Features.Payslips.CreatePayslip;
using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Payslips;
using BudgetTracker.Shared.Results;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Payslips.UpdatePayslip;

/// <summary>
/// Updates a draft payslip (TASK 8.3). A posted payslip is read-only.
/// </summary>
public class UpdatePayslipEndpoint : Endpoint<UpdatePayslipRequest, PayslipDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPayslipRepository _payslips;
    private readonly IHouseholdMemberRepository _members;
    private readonly PayslipWriteService _writer;
    private readonly PayslipDtoFactory _dtoFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public UpdatePayslipEndpoint(
        ICurrentUserService currentUser,
        IPayslipRepository payslips,
        IHouseholdMemberRepository members,
        PayslipWriteService writer,
        PayslipDtoFactory dtoFactory,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _payslips = payslips;
        _members = members;
        _writer = writer;
        _dtoFactory = dtoFactory;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Put("/api/v1/payslips/{id}");
        Validator<CreatePayslipRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(UpdatePayslipRequest req, CancellationToken ct)
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

        var result = _writer.Apply(payslip, req, userId.Value, membership?.HouseholdId);
        if (result.IsFailure)
        {
            var error = result.Errors[0];
            ThrowError(error.Message, error.Type == ErrorType.Conflict ? 409 : 400);
            return;
        }

        _payslips.Update(payslip);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = await _dtoFactory.BuildDetailAsync(payslip, userId.Value, membership?.HouseholdId, ct);
        await SendOkAsync(dto, ct);
    }
}
