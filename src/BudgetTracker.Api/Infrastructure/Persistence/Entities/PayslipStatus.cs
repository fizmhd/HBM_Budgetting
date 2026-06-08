namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Lifecycle of a payslip. A <see cref="Draft"/> can be edited freely; once <see cref="Posted"/>
/// its net pay has been turned into an income transaction and it becomes read-only.
/// </summary>
public enum PayslipStatus
{
    Draft = 0,
    Posted = 1
}
