using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Services.Interfaces;

/// <summary>
/// Seeds the default category taxonomy into a caller's scope (TASK 3.6).
/// </summary>
public interface ICategorySeeder
{
    /// <summary>
    /// Builds the default tree as <see cref="Category"/> entities (with parent links, sort order, and
    /// <c>IsSystem = true</c>) owned by <paramref name="ownerUserId"/>. When <paramref name="householdId"/>
    /// is provided the categories are created as <see cref="Visibility.HouseholdShared"/>; otherwise
    /// individual. The entities are returned un-persisted so the caller controls the unit of work.
    /// </summary>
    IReadOnlyList<Category> BuildDefaults(Guid ownerUserId, Guid? householdId);
}
