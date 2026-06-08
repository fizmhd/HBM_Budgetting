using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Payslip-specific operations (TASK 8.1).
/// </summary>
public class PayslipRepository : Repository<Payslip>, IPayslipRepository
{
    public PayslipRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<List<Payslip>> GetVisibleAsync(Guid userId, Guid? householdId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .VisibleTo(userId, householdId)
            .OrderByDescending(p => p.PayDate)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Payslip?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.LineItems)
            .Include(p => p.LeaveBalances)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Payslip>> GetForYearAsync(Guid userId, Guid? householdId, int year,
        CancellationToken cancellationToken = default)
    {
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);

        return await _dbSet
            .VisibleTo(userId, householdId)
            .Where(p => p.PayDate >= start && p.PayDate <= end)
            .Include(p => p.LineItems)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void RemoveLineItems(IEnumerable<PayslipLineItem> lineItems)
    {
        _context.Set<PayslipLineItem>().RemoveRange(lineItems);
    }

    /// <inheritdoc />
    public void RemoveLeaveBalances(IEnumerable<PayslipLeaveBalance> balances)
    {
        _context.Set<PayslipLeaveBalance>().RemoveRange(balances);
    }
}
