using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.Services;

/// <summary>
/// Implements the category management rules (TASK 3.2). The rule logic is pure over the supplied
/// scope list (so it is unit-testable); only the reference check touches the database.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    /// <summary>Error code returned when a delete is blocked by children or references.</summary>
    public const string InUseCode = "CATEGORY_IN_USE";

    /// <summary>Error code returned when a move would create a cycle.</summary>
    public const string CycleCode = "CATEGORY_CYCLE";

    /// <summary>Error code returned when a rename/create name is invalid.</summary>
    public const string NameRequiredCode = "CATEGORY_NAME_REQUIRED";

    private const int MaxNameLength = 100;

    private readonly ICategoryReferenceChecker _references;

    public CategoryService(ICategoryReferenceChecker references)
    {
        _references = references;
    }

    /// <inheritdoc />
    public Result Rename(Category category, string newName)
    {
        var trimmed = newName?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Result.Failure(Error.Validation(NameRequiredCode, "Category name is required."));
        }

        if (trimmed.Length > MaxNameLength)
        {
            return Result.Failure(Error.Validation(NameRequiredCode,
                $"Category name cannot exceed {MaxNameLength} characters."));
        }

        category.Name = trimmed;
        return Result.Success();
    }

    /// <inheritdoc />
    public Result Move(Category category, Guid? newParentId, IReadOnlyCollection<Category> scope)
    {
        if (newParentId == category.Id)
        {
            return Result.Failure(Error.Validation(CycleCode, "A category cannot be its own parent."));
        }

        if (newParentId is not null && IsDescendant(newParentId.Value, category.Id, scope))
        {
            return Result.Failure(Error.Validation(CycleCode,
                "A category cannot be moved under one of its own descendants."));
        }

        category.ParentCategoryId = newParentId;
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(Category category, IReadOnlyCollection<Category> scope,
        CancellationToken cancellationToken = default)
    {
        var hasChildren = scope.Any(c => c.ParentCategoryId == category.Id);
        if (hasChildren)
        {
            return Result.Failure(Error.Conflict(InUseCode,
                "This category has sub-categories. Move or delete them first."));
        }

        if (await _references.IsReferencedAsync(category.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict(InUseCode,
                "This category is used by one or more transactions or budgets and cannot be deleted."));
        }

        return Result.Success();
    }

    /// <summary>
    /// Walks parent links from <paramref name="candidateId"/> upward; returns true if
    /// <paramref name="ancestorId"/> is the node itself or any of its ancestors — i.e. the candidate
    /// sits inside the moved node's subtree.
    /// </summary>
    private static bool IsDescendant(Guid candidateId, Guid ancestorId, IReadOnlyCollection<Category> scope)
    {
        var byId = scope.ToDictionary(c => c.Id);
        var currentId = (Guid?)candidateId;
        var guard = 0;

        while (currentId is not null && guard++ <= scope.Count)
        {
            if (currentId.Value == ancestorId)
            {
                return true;
            }

            currentId = byId.TryGetValue(currentId.Value, out var node) ? node.ParentCategoryId : null;
        }

        return false;
    }
}
