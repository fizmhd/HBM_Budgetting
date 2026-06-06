using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for Tag-specific operations.
/// </summary>
public interface ITagRepository : IRepository<Tag>
{
    /// <summary>
    /// Lists the tags visible to the caller (own + household-shared), ordered by name.
    /// </summary>
    Task<List<Tag>> GetVisibleAsync(Guid userId, Guid? householdId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the caller's tags whose (normalised) names are in <paramref name="names"/>.
    /// </summary>
    Task<List<Tag>> GetByNamesAsync(Guid userId, IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default);
}
