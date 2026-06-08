using BudgetTracker.Shared.DTOs.Accounts;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.DTOs.Budgets;
using BudgetTracker.Shared.DTOs.Categories;
using BudgetTracker.Shared.DTOs.Dashboard;
using BudgetTracker.Shared.DTOs.Households;
using BudgetTracker.Shared.DTOs.Payslips;
using BudgetTracker.Shared.DTOs.Recurring;
using BudgetTracker.Shared.DTOs.Transactions;
using BudgetTracker.Shared.DTOs.Users;
using Refit;

namespace BudgetTracker.Web.Services;

/// <summary>
/// Refit-based API client interface for BudgetTracker API
/// </summary>
public interface IApiClient
{
    // Auth endpoints
    [Post("/api/v1/auth/register")]
    Task RegisterAsync([Body] RegisterRequest request);

    [Post("/api/v1/auth/login")]
    Task<LoginResponse?> LoginAsync([Body] LoginRequest request);

    [Post("/api/v1/auth/logout")]
    Task LogoutAsync();

    [Post("/api/v1/auth/refresh")]
    Task<RefreshTokenResponse?> RefreshTokenAsync();

    [Post("/api/v1/auth/forgot-password")]
    Task ForgotPasswordAsync([Body] ForgotPasswordRequest request);

    [Post("/api/v1/auth/reset-password")]
    Task ResetPasswordAsync([Body] ResetPasswordRequest request);

    // User endpoints
    [Get("/api/v1/users/me")]
    Task<UserDto?> GetProfileAsync();

    [Post("/api/v1/users/me/complete-profile")]
    Task<UserDto?> CompleteProfileAsync([Body] CompleteProfileRequest request);

    [Put("/api/v1/users/me")]
    Task<UserDto?> UpdateProfileAsync([Body] UpdateProfileRequest request);

    [Put("/api/v1/users/me/settings")]
    Task<UserDto?> UpdateSettingsAsync([Body] UpdateSettingsRequest request);

    // Household endpoints
    [Post("/api/v1/households")]
    Task<HouseholdDto?> CreateHouseholdAsync([Body] CreateHouseholdRequest request);

    [Get("/api/v1/households/current")]
    Task<HouseholdDto?> GetCurrentHouseholdAsync();

    [Post("/api/v1/households/{id}/invites")]
    Task<HouseholdInviteDto?> InviteMemberAsync(Guid id, [Body] InviteMemberRequest request);

    [Post("/api/v1/invites/{token}/accept")]
    Task<HouseholdDto?> AcceptInviteAsync(string token);

    [Delete("/api/v1/households/{id}/members/{memberId}")]
    Task RemoveMemberAsync(Guid id, Guid memberId);

    // Account endpoints
    [Post("/api/v1/accounts")]
    Task<AccountDto?> CreateAccountAsync([Body] CreateAccountRequest request);

    [Get("/api/v1/accounts")]
    Task<List<AccountDto>?> GetAccountsAsync();

    [Get("/api/v1/accounts/{id}")]
    Task<AccountDto?> GetAccountAsync(Guid id);

    [Put("/api/v1/accounts/{id}")]
    Task<AccountDto?> UpdateAccountAsync(Guid id, [Body] UpdateAccountRequest request);

    [Post("/api/v1/accounts/{id}/archive")]
    Task<AccountDto?> ArchiveAccountAsync(Guid id);

    // Category endpoints
    [Get("/api/v1/categories/tree")]
    Task<List<CategoryTreeNodeDto>?> GetCategoryTreeAsync();

    [Post("/api/v1/categories/seed-defaults")]
    Task<List<CategoryTreeNodeDto>?> SeedDefaultCategoriesAsync();

    [Post("/api/v1/categories")]
    Task<CategoryDto?> CreateCategoryAsync([Body] CreateCategoryRequest request);

    [Put("/api/v1/categories/{id}")]
    Task<CategoryDto?> UpdateCategoryAsync(Guid id, [Body] UpdateCategoryRequest request);

    [Put("/api/v1/categories/{id}/move")]
    Task<CategoryDto?> MoveCategoryAsync(Guid id, [Body] MoveCategoryRequest request);

    [Delete("/api/v1/categories/{id}")]
    Task DeleteCategoryAsync(Guid id);

    // Transaction endpoints
    [Get("/api/v1/transactions")]
    Task<TransactionListResponse?> GetTransactionsAsync([Query] TransactionQuery query);

    [Get("/api/v1/transactions/{id}")]
    Task<TransactionDto?> GetTransactionAsync(Guid id);

    [Post("/api/v1/transactions")]
    Task<TransactionDto?> CreateTransactionAsync([Body] CreateTransactionRequest request);

    [Put("/api/v1/transactions/{id}")]
    Task<TransactionDto?> UpdateTransactionAsync(Guid id, [Body] UpdateTransactionRequest request);

    [Delete("/api/v1/transactions/{id}")]
    Task DeleteTransactionAsync(Guid id);

    // Budget endpoints
    [Get("/api/v1/budgets")]
    Task<List<BudgetDto>?> GetBudgetsAsync([Query] BudgetQuery query);

    [Get("/api/v1/budgets/{id}")]
    Task<BudgetDto?> GetBudgetAsync(Guid id);

    [Post("/api/v1/budgets")]
    Task<BudgetDto?> CreateBudgetAsync([Body] CreateBudgetRequest request);

    [Put("/api/v1/budgets/{id}")]
    Task<BudgetDto?> UpdateBudgetAsync(Guid id, [Body] UpdateBudgetRequest request);

    [Delete("/api/v1/budgets/{id}")]
    Task DeleteBudgetAsync(Guid id);

    // Dashboard endpoint
    [Get("/api/v1/dashboard/monthly")]
    Task<MonthlyDashboardDto?> GetMonthlyDashboardAsync([Query] DashboardQuery query);

    // Recurring endpoints
    [Get("/api/v1/recurring")]
    Task<List<RecurringRuleDto>?> GetRecurringRulesAsync([Query] string? kind = null);

    [Get("/api/v1/recurring/{id}")]
    Task<RecurringRuleDto?> GetRecurringRuleAsync(Guid id);

    [Post("/api/v1/recurring")]
    Task<RecurringRuleDto?> CreateRecurringRuleAsync([Body] CreateRecurringRuleRequest request);

    [Put("/api/v1/recurring/{id}")]
    Task<RecurringRuleDto?> UpdateRecurringRuleAsync(Guid id, [Body] UpdateRecurringRuleRequest request);

    [Delete("/api/v1/recurring/{id}")]
    Task DeleteRecurringRuleAsync(Guid id);

    [Post("/api/v1/recurring/{id}/pause")]
    Task<RecurringRuleDto?> PauseRecurringRuleAsync(Guid id);

    [Post("/api/v1/recurring/{id}/resume")]
    Task<RecurringRuleDto?> ResumeRecurringRuleAsync(Guid id);

    [Post("/api/v1/recurring/generate")]
    Task<RecurringGenerationResultDto?> GenerateRecurringNowAsync();

    [Get("/api/v1/recurring/occurrences/pending")]
    Task<List<RecurringOccurrenceDto>?> GetPendingOccurrencesAsync();

    [Post("/api/v1/recurring/occurrences/{id}/skip")]
    Task<RecurringOccurrenceDto?> SkipOccurrenceAsync(Guid id, [Body] SkipOccurrenceRequest request);

    [Post("/api/v1/recurring/occurrences/{id}/confirm")]
    Task<RecurringOccurrenceDto?> ConfirmOccurrenceAsync(Guid id);

    // Payslip endpoints (Sprint 8)
    [Get("/api/v1/payslips")]
    Task<List<PayslipListItemDto>?> GetPayslipsAsync();

    [Get("/api/v1/payslips/{id}")]
    Task<PayslipDto?> GetPayslipAsync(Guid id);

    [Post("/api/v1/payslips")]
    Task<PayslipDto?> CreatePayslipAsync([Body] CreatePayslipRequest request);

    [Put("/api/v1/payslips/{id}")]
    Task<PayslipDto?> UpdatePayslipAsync(Guid id, [Body] UpdatePayslipRequest request);

    [Delete("/api/v1/payslips/{id}")]
    Task DeletePayslipAsync(Guid id);

    [Post("/api/v1/payslips/{id}/post")]
    Task<PostPayslipResultDto?> PostPayslipAsync(Guid id, [Body] PostPayslipRequest request);
}

/// <summary>
/// Query-string parameters for the monthly dashboard.
/// </summary>
public class DashboardQuery
{
    /// <summary>Target month, "yyyy-MM". Omitted = current month.</summary>
    [AliasAs("month")] public string? Month { get; set; }

    /// <summary>"individual" or "household" (default).</summary>
    [AliasAs("scope")] public string? Scope { get; set; }
}

/// <summary>
/// Query-string parameters for the budgets list (the active period window).
/// </summary>
public class BudgetQuery
{
    [AliasAs("from")] public string? From { get; set; }
    [AliasAs("to")] public string? To { get; set; }
}

/// <summary>
/// Query-string parameters for the transactions list (Refit binds public properties to query params).
/// </summary>
public class TransactionQuery
{
    [AliasAs("from")] public string? From { get; set; }
    [AliasAs("to")] public string? To { get; set; }
    [AliasAs("accountId")] public Guid? AccountId { get; set; }
    [AliasAs("noAccount")] public bool? NoAccount { get; set; }
    [AliasAs("categoryId")] public Guid? CategoryId { get; set; }
    [AliasAs("type")] public string? Type { get; set; }
    [AliasAs("tag")] public string? Tag { get; set; }
    [AliasAs("search")] public string? Search { get; set; }
    [AliasAs("sort")] public string? Sort { get; set; }
    [AliasAs("page")] public int Page { get; set; } = 1;
    [AliasAs("pageSize")] public int PageSize { get; set; } = 25;
}
