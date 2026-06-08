using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Infrastructure.Security;
using BudgetTracker.Api.Services.Payslips;
using BudgetTracker.Shared.DTOs.Payslips;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Features.Payslips;

/// <summary>
/// Shared create/update logic for payslips: validates the country/scope, parses and applies the typed
/// line items and leave balances, and encrypts the personnummer. Used by both the create and update
/// slices so the rules live in one place. Structural shape (employer required, valid dates, amounts
/// >= 0) is checked by the FluentValidation validators.
/// </summary>
public sealed class PayslipWriteService
{
    public const string CountryInvalidCode = "PAYSLIP_COUNTRY_INVALID";
    public const string LineTypeInvalidCode = "PAYSLIP_LINE_TYPE_INVALID";
    public const string SharedRequiresHouseholdCode = "PAYSLIP_SHARED_REQUIRES_HOUSEHOLD";
    public const string PostedReadOnlyCode = "PAYSLIP_POSTED_READ_ONLY";

    private readonly ICountryPayslipProfileProvider _profiles;
    private readonly IPayslipRepository _payslips;
    private readonly IFieldProtector _protector;

    public PayslipWriteService(
        ICountryPayslipProfileProvider profiles,
        IPayslipRepository payslips,
        IFieldProtector protector)
    {
        _profiles = profiles;
        _payslips = payslips;
        _protector = protector;
    }

    /// <summary>
    /// Applies <paramref name="req"/> onto <paramref name="payslip"/> (fresh or existing). Caller
    /// persists on success. A posted payslip is read-only.
    /// </summary>
    public Result Apply(Payslip payslip, CreatePayslipRequest req, Guid userId, Guid? householdId)
    {
        if (payslip.Status == PayslipStatus.Posted)
        {
            return Result.Failure(Error.Conflict(PostedReadOnlyCode,
                "A posted payslip cannot be edited."));
        }

        if (!Enum.TryParse<PayslipCountry>(req.Country, ignoreCase: true, out var country) ||
            !_profiles.Supports(country))
        {
            return Result.Failure(Error.Validation(CountryInvalidCode,
                "Country is not supported. The MVP ships Sweden only."));
        }

        if (req.IsShared && householdId is null)
        {
            return Result.Failure(Error.Validation(SharedRequiresHouseholdCode,
                "You must belong to a household to share a payslip."));
        }

        // Parse every line type up front so a bad value fails the whole write.
        var parsedLines = new List<(PayslipLineType Type, PayslipLineItemInput Input)>();
        foreach (var line in req.LineItems)
        {
            if (!Enum.TryParse<PayslipLineType>(line.Type, ignoreCase: true, out var lineType) ||
                !Enum.IsDefined(lineType))
            {
                return Result.Failure(Error.Validation(LineTypeInvalidCode,
                    $"Line type '{line.Type}' is not valid."));
            }
            parsedLines.Add((lineType, line));
        }

        // ---- Mutate the entity ----
        payslip.OwnerUserId = userId;
        payslip.Visibility = req.IsShared ? Visibility.HouseholdShared : Visibility.Individual;
        payslip.HouseholdId = req.IsShared ? householdId : null;
        payslip.Country = country;
        payslip.EmployerName = req.EmployerName.Trim();
        payslip.EmployeeName = string.IsNullOrWhiteSpace(req.EmployeeName) ? null : req.EmployeeName.Trim();
        payslip.PayPeriodStart = req.PayPeriodStart;
        payslip.PayPeriodEnd = req.PayPeriodEnd;
        payslip.PayDate = req.PayDate;
        payslip.CurrencyCode = string.IsNullOrWhiteSpace(req.CurrencyCode)
            ? "SEK"
            : req.CurrencyCode.Trim().ToUpperInvariant();
        payslip.DeclaredNet = req.DeclaredNet;
        payslip.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();

        // personnummer: encrypt + mask when supplied. On update a null/blank value leaves the stored
        // value untouched (the clear value is never returned, so the form cannot re-send it).
        if (!string.IsNullOrWhiteSpace(req.Personnummer))
        {
            var clear = req.Personnummer.Trim();
            payslip.PersonnummerEncrypted = _protector.Protect(clear);
            payslip.PersonnummerMasked = PersonnummerMasker.Mask(clear);
        }

        // Replace line items. On update the old rows are tracked, so mark them for deletion explicitly
        // (mirrors how transaction splits are replaced).
        if (payslip.LineItems.Count > 0)
        {
            _payslips.RemoveLineItems(payslip.LineItems.ToList());
            payslip.LineItems.Clear();
        }
        var order = 0;
        foreach (var (lineType, input) in parsedLines)
        {
            payslip.LineItems.Add(new PayslipLineItem
            {
                // Leave Id unset so EF treats each as a new row to INSERT.
                Type = lineType,
                Label = input.Label.Trim(),
                Quantity = input.Quantity,
                UnitAmount = input.UnitAmount,
                Amount = input.Amount,
                SortOrder = input.SortOrder == 0 ? order : input.SortOrder
            });
            order++;
        }

        // Replace leave balances.
        if (payslip.LeaveBalances.Count > 0)
        {
            _payslips.RemoveLeaveBalances(payslip.LeaveBalances.ToList());
            payslip.LeaveBalances.Clear();
        }
        foreach (var balance in req.LeaveBalances)
        {
            if (string.IsNullOrWhiteSpace(balance.LeaveType))
            {
                continue;
            }
            payslip.LeaveBalances.Add(new PayslipLeaveBalance
            {
                LeaveType = balance.LeaveType.Trim(),
                Balance = balance.Balance,
                Unit = string.IsNullOrWhiteSpace(balance.Unit) ? "days" : balance.Unit.Trim()
            });
        }

        return Result.Success();
    }
}
