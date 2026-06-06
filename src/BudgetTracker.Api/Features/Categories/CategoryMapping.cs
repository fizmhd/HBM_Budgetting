using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.DTOs.Categories;

namespace BudgetTracker.Api.Features.Categories;

/// <summary>
/// Mapping helpers between Category entities and DTOs, including the flat-to-nested tree build.
/// </summary>
public static class CategoryMapping
{
    public static CategoryDto ToDto(this Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        ParentCategoryId = category.ParentCategoryId,
        SortOrder = category.SortOrder,
        Icon = category.Icon,
        IsSystem = category.IsSystem,
        IsShared = category.Visibility == Visibility.HouseholdShared
    };

    /// <summary>
    /// Builds the nested tree from a flat, scope-filtered list. Orphan nodes (whose parent is not in
    /// the visible set) are surfaced as roots so nothing is silently hidden.
    /// </summary>
    public static List<CategoryTreeNodeDto> BuildTree(IReadOnlyCollection<Category> categories)
    {
        var nodes = categories.ToDictionary(c => c.Id, c => new CategoryTreeNodeDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentCategoryId = c.ParentCategoryId,
            SortOrder = c.SortOrder,
            Icon = c.Icon,
            IsSystem = c.IsSystem,
            IsShared = c.Visibility == Visibility.HouseholdShared
        });

        var roots = new List<CategoryTreeNodeDto>();
        foreach (var node in nodes.Values)
        {
            if (node.ParentCategoryId is { } parentId && nodes.TryGetValue(parentId, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        Sort(roots);
        return roots;
    }

    private static void Sort(List<CategoryTreeNodeDto> nodes)
    {
        nodes.Sort((a, b) =>
        {
            var bySort = a.SortOrder.CompareTo(b.SortOrder);
            return bySort != 0 ? bySort : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

        foreach (var node in nodes)
        {
            Sort(node.Children);
        }
    }
}
