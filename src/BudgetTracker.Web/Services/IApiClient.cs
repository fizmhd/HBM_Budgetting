using BudgetTracker.Shared.DTOs.Accounts;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.DTOs.Categories;
using BudgetTracker.Shared.DTOs.Households;
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
}

/// <summary>
/// Query-string parameters for the transactions list (Refit binds public properties to query params).
/// </summary>
public class TransactionQuery
{
    [AliasAs("from")] public string? From { get; set; }
    [AliasAs("to")] public string? To { get; set; }
    [AliasAs("accountId")] public Guid? AccountId { get; set; }
    [AliasAs("categoryId")] public Guid? CategoryId { get; set; }
    [AliasAs("type")] public string? Type { get; set; }
    [AliasAs("tag")] public string? Tag { get; set; }
    [AliasAs("search")] public string? Search { get; set; }
    [AliasAs("sort")] public string? Sort { get; set; }
    [AliasAs("page")] public int Page { get; set; } = 1;
    [AliasAs("pageSize")] public int PageSize { get; set; } = 25;
}
