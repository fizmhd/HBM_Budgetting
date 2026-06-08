namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// One payslip (Swedish <i>lönespecifikation</i>) for a single pay period (TASK 8.1): a meta header,
/// a set of typed <see cref="PayslipLineItem"/>s, the printed net to reconcile against, and any
/// <see cref="PayslipLeaveBalance"/>s. Owned by a user and optionally shared with their household
/// (via <see cref="OwnedEntity.Visibility"/>). Once posted (TASK 8.4) its net becomes an income
/// transaction linked through <see cref="PostedTransactionId"/>.
/// </summary>
public class Payslip : OwnedEntity
{
    /// <summary>Country profile this payslip is read with (reconciliation rules + summary labels).</summary>
    public PayslipCountry Country { get; set; } = PayslipCountry.Sweden;

    /// <summary>Employer name as printed on the payslip.</summary>
    public string EmployerName { get; set; } = string.Empty;

    /// <summary>Employee name as printed on the payslip (optional).</summary>
    public string? EmployeeName { get; set; }

    /// <summary>
    /// Swedish personnummer, encrypted at rest (base64 of the protected bytes). Sensitive personal
    /// data (GDPR): never logged and never returned to the client in clear — only
    /// <see cref="PersonnummerMasked"/> is exposed. Null when not supplied.
    /// </summary>
    public string? PersonnummerEncrypted { get; set; }

    /// <summary>
    /// Display-safe mask of the personnummer (e.g. <c>"19900101-****"</c>), computed once at write time
    /// so reads/lists never decrypt. Null when no personnummer was supplied.
    /// </summary>
    public string? PersonnummerMasked { get; set; }

    /// <summary>First calendar day of the pay period.</summary>
    public DateOnly PayPeriodStart { get; set; }

    /// <summary>Last calendar day of the pay period.</summary>
    public DateOnly PayPeriodEnd { get; set; }

    /// <summary>Date the net pay is/was disbursed (used as the posted transaction's date).</summary>
    public DateOnly PayDate { get; set; }

    /// <summary>ISO currency code; "SEK" in the MVP.</summary>
    public string CurrencyCode { get; set; } = "SEK";

    /// <summary>
    /// Net pay as printed on the payslip. The reconciliation calc compares the net computed from the
    /// line items against this value (acceptance: "reconciles to net").
    /// </summary>
    public decimal DeclaredNet { get; set; }

    /// <summary>Free-text notes.</summary>
    public string? Notes { get; set; }

    public PayslipStatus Status { get; set; } = PayslipStatus.Draft;

    /// <summary>The income transaction created when the payslip was posted (null while a draft).</summary>
    public Guid? PostedTransactionId { get; set; }

    /// <summary>The account the net pay was posted to (null while a draft).</summary>
    public Guid? PostedAccountId { get; set; }

    /// <summary>UTC time the payslip was posted (null while a draft).</summary>
    public DateTime? PostedAt { get; set; }

    /// <summary>Typed line items the summary and reconciliation are computed from.</summary>
    public List<PayslipLineItem> LineItems { get; set; } = new();

    /// <summary>Leave balances printed on the payslip (e.g. remaining vacation days).</summary>
    public List<PayslipLeaveBalance> LeaveBalances { get; set; } = new();
}
