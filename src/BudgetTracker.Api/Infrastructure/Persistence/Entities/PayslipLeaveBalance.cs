namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// A leave balance printed on a payslip (TASK 8.1), e.g. remaining vacation days (<c>Semester</c>),
/// owned by its <see cref="Payslip"/>. Entered manually and displayed; not otherwise computed.
/// </summary>
public class PayslipLeaveBalance : BaseEntity
{
    public Guid PayslipId { get; set; }

    /// <summary>Kind of leave as printed (e.g. "Semester", "Sparade dagar").</summary>
    public string LeaveType { get; set; } = string.Empty;

    /// <summary>Remaining balance.</summary>
    public decimal Balance { get; set; }

    /// <summary>Unit the balance is measured in (e.g. "days").</summary>
    public string Unit { get; set; } = "days";
}
