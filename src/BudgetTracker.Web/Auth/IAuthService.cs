using BudgetTracker.Shared.DTOs.Auth;

namespace BudgetTracker.Web.Auth;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<string?> RefreshTokenAsync();
    Task<UserDto?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
}
