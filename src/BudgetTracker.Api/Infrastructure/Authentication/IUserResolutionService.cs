using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Service for resolving external auth provider users to internal User entities
/// </summary>
public interface IUserResolutionService
{
    /// <summary>
    /// Resolves an external user to an internal User entity.
    /// Creates user and/or external login link if needed.
    /// </summary>
    /// <param name="provider">Auth provider name (e.g., "supabase")</param>
    /// <param name="providerKey">External user ID from the provider</param>
    /// <param name="email">User's email address</param>
    Task<Result<User>> ResolveUserAsync(string provider, string providerKey, string email);
}
