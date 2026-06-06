using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services.Interfaces;

namespace BudgetTracker.Api.Services.Categories;

/// <summary>
/// Materialises <see cref="DefaultCategoryTaxonomy"/> into owned <see cref="Category"/> entities.
/// </summary>
public sealed class CategorySeeder : ICategorySeeder
{
    /// <inheritdoc />
    public IReadOnlyList<Category> BuildDefaults(Guid ownerUserId, Guid? householdId)
    {
        var visibility = householdId is not null ? Visibility.HouseholdShared : Visibility.Individual;
        var result = new List<Category>();

        var order = 0;
        foreach (var root in DefaultCategoryTaxonomy.Roots)
        {
            AddRecursive(root, parentId: null, order++, ownerUserId, householdId, visibility, result);
        }

        return result;
    }

    private static void AddRecursive(SeedCategory spec, Guid? parentId, int sortOrder, Guid ownerUserId,
        Guid? householdId, Visibility visibility, List<Category> sink)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = spec.Name,
            ParentCategoryId = parentId,
            SortOrder = sortOrder,
            IsSystem = true,
            OwnerUserId = ownerUserId,
            Visibility = visibility,
            HouseholdId = householdId
        };
        sink.Add(category);

        var childOrder = 0;
        foreach (var child in spec.Children)
        {
            AddRecursive(child, category.Id, childOrder++, ownerUserId, householdId, visibility, sink);
        }
    }
}
