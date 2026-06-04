using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Service for accessing the current authenticated user
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID, or null if not authenticated
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the full user entity (lazy-loaded)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user entity, or null if not authenticated</returns>
    Task<User?> GetUserAsync(CancellationToken cancellationToken = default);
}
