using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Payslip-specific operations (TASK 8.1).
/// </summary>
public interface IPayslipRepository : IRepository<Payslip>
{
    /// <summary>
    /// Lists the payslips visible to the caller (own + household-shared), newest pay date first.
    /// Line items and leave balances are <b>not</b> loaded (list view shows the header only).
    /// </summary>
    Task<List<Payslip>> GetVisibleAsync(Guid userId, Guid? householdId,
        CancellationToken cancellationToken = default);

    /// <summary>Loads a payslip with its line items and leave balances.</summary>
    Task<Payslip?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads (with line items) the caller-visible payslips whose pay date falls in
    /// <paramref name="year"/>. Backs the year-to-date summary, which aggregates the line items of
    /// every payslip in the same calendar year.
    /// </summary>
    Task<List<Payslip>> GetForYearAsync(Guid userId, Guid? householdId, int year,
        CancellationToken cancellationToken = default);

    /// <summary>Removes the given line items (used when replacing a payslip's lines on update).</summary>
    void RemoveLineItems(IEnumerable<PayslipLineItem> lineItems);

    /// <summary>Removes the given leave balances (used when replacing them on update).</summary>
    void RemoveLeaveBalances(IEnumerable<PayslipLeaveBalance> balances);
}
