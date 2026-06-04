using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository interface for UserExternalLogin-specific operations
/// </summary>
public interface IUserExternalLoginRepository : IRepository<UserExternalLogin>
{
    /// <summary>
    /// Finds an external login by provider and provider key
    /// </summary>
    /// <param name="provider">The authentication provider name</param>
    /// <param name="providerKey">The unique identifier from the provider</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>The external login if found, otherwise null</returns>
    Task<UserExternalLogin?> GetByProviderAsync(
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all external logins for a specific user
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <param name="cancellationToken">A token to cancel the operation</param>
    /// <returns>A list of external logins for the user</returns>
    Task<List<UserExternalLogin>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
