using BudgetTracker.Shared.DTOs.Auth;
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
}
