namespace BudgetTracker.Api.Infrastructure.Persistence.Entities;

/// <summary>
/// A node in the user's spending/income taxonomy. Categories form a self-referencing tree
/// (<see cref="ParentCategoryId"/> = null marks a root). Owned by a user and optionally shared with
/// their household (via <see cref="OwnedEntity.Visibility"/>).
/// </summary>
public class Category : OwnedEntity
{
    /// <summary>
    /// Display name of the category.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parent node in the tree, or null when this is a root category.
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>
    /// Sort position among siblings (ascending).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Optional icon identifier (e.g. a Bootstrap icon name); purely presentational.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// True for seeded structural defaults. Protected from deletion of the structure in some flows,
    /// but the user may still rename/extend them.
    /// </summary>
    public bool IsSystem { get; set; }
}
