namespace BudgetTracker.Shared.DTOs.Payslips;

/// <summary>A single typed line on a payslip.</summary>
public class PayslipLineItemDto
{
    public Guid Id { get; set; }

    /// <summary>"Earning", "Benefit", "Tax", "Deduction", "Reimbursement", or "Info".</summary>
    public string Type { get; set; } = "Earning";

    public string Label { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? UnitAmount { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>A payslip line as entered/edited.</summary>
public class PayslipLineItemInput
{
    /// <summary>"Earning", "Benefit", "Tax", "Deduction", "Reimbursement", or "Info".</summary>
    public string Type { get; set; } = "Earning";

    public string Label { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? UnitAmount { get; set; }
    public decimal Amount { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>A leave balance printed on the payslip (e.g. remaining vacation days).</summary>
public class PayslipLeaveBalanceDto
{
    public Guid Id { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Unit { get; set; } = "days";
}

/// <summary>A leave balance as entered/edited.</summary>
public class PayslipLeaveBalanceInput
{
    public string LeaveType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Unit { get; set; } = "days";
}

/// <summary>
/// The gross/benefits/tax/net totals derived from a payslip's line items, with the country profile's
/// localized labels. Used for both the month and year-to-date summaries.
/// </summary>
public class PayslipSummaryDto
{
    public decimal Gross { get; set; }
    public decimal Benefits { get; set; }
    public decimal Tax { get; set; }
    public decimal Deductions { get; set; }
    public decimal Reimbursements { get; set; }
    public decimal Net { get; set; }

    // Localized labels from the country profile (e.g. "Bruttolön", "Förmåner", "Skatt", "Nettolön").
    public string GrossLabel { get; set; } = "Gross";
    public string BenefitsLabel { get; set; } = "Benefits";
    public string TaxLabel { get; set; } = "Tax";
    public string NetLabel { get; set; } = "Net";
}

/// <summary>Comparison of the computed net against the net printed on the payslip.</summary>
public class PayslipReconciliationDto
{
    public decimal ComputedNet { get; set; }
    public decimal DeclaredNet { get; set; }

    /// <summary>Computed net minus declared net (0 when they agree).</summary>
    public decimal Difference { get; set; }

    public bool IsReconciled { get; set; }
}

/// <summary>Lightweight payslip header for the list view (no line items).</summary>
public class PayslipListItemDto
{
    public Guid Id { get; set; }
    public string Country { get; set; } = "Sweden";
    public string EmployerName { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public DateOnly PayPeriodStart { get; set; }
    public DateOnly PayPeriodEnd { get; set; }
    public DateOnly PayDate { get; set; }
    public string CurrencyCode { get; set; } = "SEK";
    public decimal DeclaredNet { get; set; }

    /// <summary>"Draft" or "Posted".</summary>
    public string Status { get; set; } = "Draft";

    public Guid? PostedTransactionId { get; set; }
    public bool IsShared { get; set; }
}

/// <summary>A full payslip with its line items, leave balances, summaries and reconciliation.</summary>
public class PayslipDto
{
    public Guid Id { get; set; }
    public string Country { get; set; } = "Sweden";

    public string EmployerName { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }

    /// <summary>Masked personnummer for display (e.g. "19900101-****"); the clear value is never returned.</summary>
    public string? PersonnummerMasked { get; set; }

    public DateOnly PayPeriodStart { get; set; }
    public DateOnly PayPeriodEnd { get; set; }
    public DateOnly PayDate { get; set; }

    public string CurrencyCode { get; set; } = "SEK";
    public decimal DeclaredNet { get; set; }
    public string? Notes { get; set; }

    /// <summary>"Draft" or "Posted".</summary>
    public string Status { get; set; } = "Draft";

    public Guid? PostedTransactionId { get; set; }
    public Guid? PostedAccountId { get; set; }
    public string? PostedAccountName { get; set; }
    public DateTime? PostedAt { get; set; }

    public bool IsShared { get; set; }

    public List<PayslipLineItemDto> LineItems { get; set; } = new();
    public List<PayslipLeaveBalanceDto> LeaveBalances { get; set; } = new();

    /// <summary>Totals for this pay period, computed from the line items.</summary>
    public PayslipSummaryDto Summary { get; set; } = new();

    /// <summary>Totals for the whole pay-date year, summed across the owner's payslips.</summary>
    public PayslipSummaryDto YearToDateSummary { get; set; } = new();

    /// <summary>Whether the computed net matches the printed net.</summary>
    public PayslipReconciliationDto Reconciliation { get; set; } = new();
}

/// <summary>Request to create a payslip.</summary>
public class CreatePayslipRequest
{
    /// <summary>Country profile, e.g. "Sweden".</summary>
    public string Country { get; set; } = "Sweden";

    public string EmployerName { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }

    /// <summary>
    /// Clear personnummer (write-only, encrypted at rest). On update, null/blank leaves the stored
    /// value unchanged. Never echoed back.
    /// </summary>
    public string? Personnummer { get; set; }

    public DateOnly PayPeriodStart { get; set; }
    public DateOnly PayPeriodEnd { get; set; }
    public DateOnly PayDate { get; set; }

    public string CurrencyCode { get; set; } = "SEK";

    /// <summary>Net pay as printed on the payslip; the reconciliation target.</summary>
    public decimal DeclaredNet { get; set; }

    public string? Notes { get; set; }
    public bool IsShared { get; set; }

    public List<PayslipLineItemInput> LineItems { get; set; } = new();
    public List<PayslipLeaveBalanceInput> LeaveBalances { get; set; } = new();
}

/// <summary>Request to update a payslip. Same shape as create.</summary>
public class UpdatePayslipRequest : CreatePayslipRequest
{
}

/// <summary>Request to post a payslip's net pay as an income transaction (TASK 8.4).</summary>
public class PostPayslipRequest
{
    /// <summary>Account the net pay lands in (required).</summary>
    public Guid AccountId { get; set; }

    /// <summary>Income category for the generated transaction's split (required).</summary>
    public Guid CategoryId { get; set; }

    /// <summary>Optional transaction date; defaults to the payslip's pay date.</summary>
    public DateOnly? Date { get; set; }
}

/// <summary>Result of posting a payslip.</summary>
public class PostPayslipResultDto
{
    public Guid PayslipId { get; set; }
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Posted";
}
