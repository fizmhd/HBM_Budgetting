using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Infrastructure.Persistence;

/// <summary>
/// Query helpers that enforce the owner/visibility privacy rule for <see cref="OwnedEntity"/> records.
/// </summary>
public static class OwnedEntityQueryExtensions
{
    /// <summary>
    /// Restricts a query to the records the given user is allowed to see:
    /// records they own, plus records explicitly shared with their household.
    /// Composes into EF Core queries (translated to SQL) and works over in-memory queryables.
    /// </summary>
    /// <typeparam name="T">An owned entity type.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="userId">The current user's internal id.</param>
    /// <param name="householdId">The current user's household id, or null if they have none.</param>
    public static IQueryable<T> VisibleTo<T>(this IQueryable<T> query, Guid userId, Guid? householdId)
        where T : OwnedEntity
    {
        return query.Where(e =>
            e.OwnerUserId == userId ||
            (e.Visibility == Visibility.HouseholdShared && householdId != null && e.HouseholdId == householdId));
    }

    /// <summary>
    /// True if the given user may read/write the record under the privacy rule.
    /// </summary>
    public static bool IsVisibleTo(this OwnedEntity entity, Guid userId, Guid? householdId)
    {
        return entity.OwnerUserId == userId ||
            (entity.Visibility == Visibility.HouseholdShared && householdId != null && entity.HouseholdId == householdId);
    }
}
