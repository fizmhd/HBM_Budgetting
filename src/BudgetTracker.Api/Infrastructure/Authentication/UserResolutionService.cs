using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Shared.Results;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Service for resolving external auth provider users to internal User entities
/// </summary>
public class UserResolutionService : IUserResolutionService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserExternalLoginRepository _externalLoginRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserResolutionService> _logger;

    public UserResolutionService(
        IUserRepository userRepository,
        IUserExternalLoginRepository externalLoginRepository,
        IUnitOfWork unitOfWork,
        ILogger<UserResolutionService> logger)
    {
        _userRepository = userRepository;
        _externalLoginRepository = externalLoginRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<User>> ResolveUserAsync(string provider, string providerKey, string email)
    {
        try
        {
            // Step 1: Look up UserExternalLogin by provider + providerKey
            var externalLogin = await _externalLoginRepository.GetByProviderAsync(provider, providerKey);

            if (externalLogin != null)
            {
                // Found existing external login, load the associated user
                var existingUser = await _userRepository.GetByIdAsync(externalLogin.UserId);
                if (existingUser == null)
                {
                    _logger.LogError("External login found but user {UserId} does not exist", externalLogin.UserId);
                    return Result<User>.Failure(Error.Internal("DATA_INTEGRITY_ERROR", "User account not found"));
                }

                // Update last login timestamp
                externalLogin.LastLoginAt = DateTime.UtcNow;
                _externalLoginRepository.Update(externalLogin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Resolved existing user {UserId} via external login", existingUser.Id);
                return Result<User>.Success(existingUser);
            }

            // Step 2: External login not found, check if User exists by email
            var userByEmail = await _userRepository.GetByEmailAsync(email);

            if (userByEmail != null)
            {
                // User exists, create external login link
                var newExternalLogin = new UserExternalLogin
                {
                    UserId = userByEmail.Id,
                    Provider = provider,
                    ProviderKey = providerKey,
                    ProviderEmail = email,
                    LastLoginAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _externalLoginRepository.AddAsync(newExternalLogin);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Linked existing user {UserId} to external provider {Provider}", userByEmail.Id, provider);
                return Result<User>.Success(userByEmail);
            }

            // Step 3: User doesn't exist, create new User + UserExternalLogin
            var newUser = new User
            {
                Email = email,
                IsProfileComplete = false,
                IsActive = true,
                TokenVersion = 1,
                FailedLoginAttempts = 0
            };

            await _userRepository.AddAsync(newUser);

            var newUserExternalLogin = new UserExternalLogin
            {
                UserId = newUser.Id,
                Provider = provider,
                ProviderKey = providerKey,
                ProviderEmail = email,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true
            };

            await _externalLoginRepository.AddAsync(newUserExternalLogin);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created new user {UserId} and linked to external provider {Provider}", newUser.Id, provider);
            return Result<User>.Success(newUser);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while resolving user for provider {Provider}, key {ProviderKey}", provider, providerKey);
            return Result<User>.Failure(Error.Internal("DATABASE_ERROR", "Failed to resolve user account"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while resolving user for provider {Provider}, key {ProviderKey}", provider, providerKey);
            return Result<User>.Failure(Error.Internal("UNEXPECTED_ERROR", "An unexpected error occurred while resolving user"));
        }
    }
}
