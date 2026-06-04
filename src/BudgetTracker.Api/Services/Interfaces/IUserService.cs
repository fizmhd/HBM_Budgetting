using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.DTOs.Users;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// Interface for user management service
/// </summary>
public interface IUserService
{
    Task<Result<UserDto>> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateSettingsAsync(UpdateSettingsRequest request, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CompleteProfileAsync(CompleteProfileRequest request, CancellationToken cancellationToken = default);
}
