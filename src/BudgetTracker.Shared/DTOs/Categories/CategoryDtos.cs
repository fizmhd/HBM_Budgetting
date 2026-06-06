namespace BudgetTracker.Shared.DTOs.Categories;

/// <summary>
/// A single category visible to the caller (flat representation).
/// </summary>
public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
    public string? Icon { get; set; }
    public bool IsSystem { get; set; }

    /// <summary>
    /// True when shared with the household; otherwise individual/private.
    /// </summary>
    public bool IsShared { get; set; }
}

/// <summary>
/// A category node with its children nested, for the tree endpoint.
/// </summary>
public class CategoryTreeNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public int SortOrder { get; set; }
    public string? Icon { get; set; }
    public bool IsSystem { get; set; }
    public bool IsShared { get; set; }

    /// <summary>
    /// Child categories, ordered by sort order then name.
    /// </summary>
    public List<CategoryTreeNodeDto> Children { get; set; } = new();

    /// <summary>
    /// Convenience flag: a node with no children. Leaf nodes (and leaf parents) are selectable in the
    /// picker; a parent that still has children is not directly selectable.
    /// </summary>
    public bool IsLeaf => Children.Count == 0;
}

/// <summary>
/// Request to create a category.
/// </summary>
public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// Share the category with the household instead of keeping it individual.
    /// </summary>
    public bool IsShared { get; set; }
}

/// <summary>
/// Request to update a category's editable fields (rename / icon / sort).
/// </summary>
public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Request to re-parent a category. <see cref="NewParentId"/> null makes it a root.
/// </summary>
public class MoveCategoryRequest
{
    public Guid? NewParentId { get; set; }
}
