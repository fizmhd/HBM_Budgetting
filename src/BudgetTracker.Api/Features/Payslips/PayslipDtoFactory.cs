using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Payslips;
using BudgetTracker.Shared.DTOs.Payslips;

namespace BudgetTracker.Api.Features.Payslips;

/// <summary>
/// Builds the full <see cref="PayslipDto"/> for a payslip: the month summary and reconciliation from
/// the payslip's own line items, plus a year-to-date summary aggregated across every payslip the
/// caller can see in the same pay-date year (acceptance: "Summary/YTD ... auto-compute from line
/// items"). Resolves the posted account name where applicable.
/// </summary>
public sealed class PayslipDtoFactory
{
    private readonly ICountryPayslipProfileProvider _profiles;
    private readonly IPayslipRepository _payslips;
    private readonly IAccountRepository _accounts;

    public PayslipDtoFactory(
        ICountryPayslipProfileProvider profiles,
        IPayslipRepository payslips,
        IAccountRepository accounts)
    {
        _profiles = profiles;
        _payslips = payslips;
        _accounts = accounts;
    }

    /// <summary>
    /// Builds the detail DTO. <paramref name="payslip"/> must have its line items and leave balances
    /// loaded (or attached in-memory after a write).
    /// </summary>
    public async Task<PayslipDto> BuildDetailAsync(Payslip payslip, Guid userId, Guid? householdId,
        CancellationToken ct)
    {
        var profile = _profiles.Get(payslip.Country);
        var labels = profile.Labels;

        var summary = profile.Summarize(payslip.LineItems);
        var reconciliation = profile.Reconcile(payslip.LineItems, payslip.DeclaredNet);

        // Year-to-date: sum the month summaries of every visible payslip sharing this pay-date year
        // (this payslip included — when freshly created it may not be persisted yet, so substitute it).
        var yearPayslips = await _payslips.GetForYearAsync(userId, householdId, payslip.PayDate.Year, ct);
        var ytdLines = yearPayslips
            .Where(p => p.Id != payslip.Id)
            .SelectMany(p => p.LineItems)
            .Concat(payslip.LineItems)
            .ToList();
        var ytdSummary = profile.Summarize(ytdLines);

        string? postedAccountName = null;
        if (payslip.PostedAccountId is { } accountId)
        {
            var account = await _accounts.GetByIdAsync(accountId, ct);
            postedAccountName = account?.Name;
        }

        return new PayslipDto
        {
            Id = payslip.Id,
            Country = payslip.Country.ToString(),
            EmployerName = payslip.EmployerName,
            EmployeeName = payslip.EmployeeName,
            PersonnummerMasked = payslip.PersonnummerMasked,
            PayPeriodStart = payslip.PayPeriodStart,
            PayPeriodEnd = payslip.PayPeriodEnd,
            PayDate = payslip.PayDate,
            CurrencyCode = payslip.CurrencyCode,
            DeclaredNet = payslip.DeclaredNet,
            Notes = payslip.Notes,
            Status = payslip.Status.ToString(),
            PostedTransactionId = payslip.PostedTransactionId,
            PostedAccountId = payslip.PostedAccountId,
            PostedAccountName = postedAccountName,
            PostedAt = payslip.PostedAt,
            IsShared = payslip.Visibility == Visibility.HouseholdShared,
            LineItems = payslip.LineItems
                .OrderBy(l => l.SortOrder)
                .Select(l => l.ToDto())
                .ToList(),
            LeaveBalances = payslip.LeaveBalances
                .Select(b => b.ToDto())
                .ToList(),
            Summary = summary.ToDto(labels),
            YearToDateSummary = ytdSummary.ToDto(labels),
            Reconciliation = reconciliation.ToDto()
        };
    }
}
