using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services.Payslips;
using BudgetTracker.Shared.DTOs.Payslips;

namespace BudgetTracker.Api.Features.Payslips;

/// <summary>
/// Mapping helpers between payslip entities and DTOs.
/// </summary>
public static class PayslipMapping
{
    public static PayslipListItemDto ToListItemDto(this Payslip p) => new()
    {
        Id = p.Id,
        Country = p.Country.ToString(),
        EmployerName = p.EmployerName,
        EmployeeName = p.EmployeeName,
        PayPeriodStart = p.PayPeriodStart,
        PayPeriodEnd = p.PayPeriodEnd,
        PayDate = p.PayDate,
        CurrencyCode = p.CurrencyCode,
        DeclaredNet = p.DeclaredNet,
        Status = p.Status.ToString(),
        PostedTransactionId = p.PostedTransactionId,
        IsShared = p.Visibility == Visibility.HouseholdShared
    };

    public static PayslipLineItemDto ToDto(this PayslipLineItem l) => new()
    {
        Id = l.Id,
        Type = l.Type.ToString(),
        Label = l.Label,
        Quantity = l.Quantity,
        UnitAmount = l.UnitAmount,
        Amount = l.Amount,
        SortOrder = l.SortOrder
    };

    public static PayslipLeaveBalanceDto ToDto(this PayslipLeaveBalance b) => new()
    {
        Id = b.Id,
        LeaveType = b.LeaveType,
        Balance = b.Balance,
        Unit = b.Unit
    };

    /// <summary>Maps a computed summary plus the profile's labels to its DTO.</summary>
    public static PayslipSummaryDto ToDto(this PayslipSummary s, PayslipLabels labels) => new()
    {
        Gross = s.Gross,
        Benefits = s.Benefits,
        Tax = s.Tax,
        Deductions = s.Deductions,
        Reimbursements = s.Reimbursements,
        Net = s.Net,
        GrossLabel = labels.Gross,
        BenefitsLabel = labels.Benefits,
        TaxLabel = labels.Tax,
        NetLabel = labels.Net
    };

    public static PayslipReconciliationDto ToDto(this PayslipReconciliation r) => new()
    {
        ComputedNet = r.Summary.Net,
        DeclaredNet = r.DeclaredNet,
        Difference = r.Difference,
        IsReconciled = r.IsReconciled
    };
}
