using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Payslips;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Payslips.CreatePayslip;

/// <summary>
/// Creates a payslip for the caller (TASK 8.3).
/// </summary>
public class CreatePayslipEndpoint : Endpoint<CreatePayslipRequest, PayslipDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPayslipRepository _payslips;
    private readonly IHouseholdMemberRepository _members;
    private readonly PayslipWriteService _writer;
    private readonly PayslipDtoFactory _dtoFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public CreatePayslipEndpoint(
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
        Post("/api/v1/payslips");
        Validator<CreatePayslipRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CreatePayslipRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);

        var payslip = new Payslip { Id = Guid.NewGuid() };
        var result = _writer.Apply(payslip, req, userId.Value, membership?.HouseholdId);
        if (result.IsFailure)
        {
            ThrowError(result.Errors[0].Message, 400);
            return;
        }

        await _payslips.AddAsync(payslip, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = await _dtoFactory.BuildDetailAsync(payslip, userId.Value, membership?.HouseholdId, ct);
        await SendOkAsync(dto, ct);
    }
}
