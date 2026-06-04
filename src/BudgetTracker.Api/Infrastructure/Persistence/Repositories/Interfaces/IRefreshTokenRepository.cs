using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository interface for RefreshToken-specific operations
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    /// <summary>
    /// Finds a refresh token by its hash
    /// </summary>
    /// <param name="hash">The token hash to search for</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>The refresh token if found, otherwise null</returns>
    Task<RefreshToken?> GetByTokenHashAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active refresh tokens for a specific user
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A list of active refresh tokens for the user</returns>
    Task<List<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refresh tokens in a specific family
    /// </summary>
    /// <param name="familyId">The family ID</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A list of refresh tokens in the family</returns>
    Task<List<RefreshToken>> GetByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a specific user
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens in a specific family
    /// </summary>
    /// <param name="familyId">The family ID</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    Task RevokeByFamilyIdAsync(Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all expired refresh tokens
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>The number of tokens deleted</returns>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of active refresh tokens for a specific user
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>The count of active refresh tokens</returns>
    Task<int> CountActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
