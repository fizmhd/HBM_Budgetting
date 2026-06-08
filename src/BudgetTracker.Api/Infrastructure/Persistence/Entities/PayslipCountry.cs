namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// Country profile a payslip is entered against (TASK 8.2). The payslip shape itself is
/// country-agnostic (meta + typed line items + summary + leave); the country selects the
/// reconciliation rules and localized summary labels. Sweden is the first profile — others
/// (Nordic) can slot in later (D10) without a model change.
/// </summary>
public enum PayslipCountry
{
    Sweden = 1
}
