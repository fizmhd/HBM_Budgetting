using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for HouseholdInvite-specific operations.
/// </summary>
public interface IHouseholdInviteRepository : IRepository<HouseholdInvite>
{
    /// <summary>
    /// Gets an invite by its token, with the household eagerly loaded.
    /// </summary>
    Task<HouseholdInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
}
