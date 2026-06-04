namespace BudgetTracker.Web.Services;

public class CsrfOptions
{
    public string CookieName { get; set; } = "X-CSRF-TOKEN";
    public string HeaderName { get; set; } = "X-CSRF-TOKEN";
}
