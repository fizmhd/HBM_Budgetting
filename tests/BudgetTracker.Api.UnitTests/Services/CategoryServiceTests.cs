using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services;
using BudgetTracker.Api.Services.Interfaces;
using FluentAssertions;
using NSubstitute;

namespace BudgetTracker.Api.UnitTests.Services;

/// <summary>
/// Unit tests for the category management rules (TASK 3.2): rename, move/cycle, and delete blocking.
/// </summary>
public class CategoryServiceTests
{
    private readonly ICategoryReferenceChecker _references = Substitute.For<ICategoryReferenceChecker>();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        // Default: not referenced by any transaction/budget.
        _references.IsReferencedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        _service = new CategoryService(_references);
    }

    private static Category Cat(Guid id, Guid? parent = null, string name = "Cat") =>
        new() { Id = id, ParentCategoryId = parent, Name = name };

    [Fact]
    public void Rename_with_a_valid_name_succeeds_and_trims()
    {
        var category = Cat(Guid.NewGuid());

        var result = _service.Rename(category, "  Groceries  ");

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Groceries");
    }

    [Fact]
    public void Rename_with_an_empty_name_fails()
    {
        var category = Cat(Guid.NewGuid(), name: "Original");

        var result = _service.Rename(category, "   ");

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(CategoryService.NameRequiredCode);
        category.Name.Should().Be("Original");
    }

    [Fact]
    public void Move_to_a_new_parent_succeeds()
    {
        var parent = Cat(Guid.NewGuid());
        var child = Cat(Guid.NewGuid());
        var scope = new[] { parent, child };

        var result = _service.Move(child, parent.Id, scope);

        result.IsSuccess.Should().BeTrue();
        child.ParentCategoryId.Should().Be(parent.Id);
    }

    [Fact]
    public void Move_under_self_is_blocked_as_a_cycle()
    {
        var category = Cat(Guid.NewGuid());

        var result = _service.Move(category, category.Id, new[] { category });

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(CategoryService.CycleCode);
    }

    [Fact]
    public void Move_under_a_descendant_is_blocked_as_a_cycle()
    {
        // root -> mid -> leaf. Moving root under leaf would create a cycle.
        var root = Cat(Guid.NewGuid(), name: "root");
        var mid = Cat(Guid.NewGuid(), root.Id, "mid");
        var leaf = Cat(Guid.NewGuid(), mid.Id, "leaf");
        var scope = new[] { root, mid, leaf };

        var result = _service.Move(root, leaf.Id, scope);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(CategoryService.CycleCode);
        root.ParentCategoryId.Should().BeNull();
    }

    [Fact]
    public async Task Delete_a_leaf_with_no_references_succeeds()
    {
        var leaf = Cat(Guid.NewGuid());

        var result = await _service.DeleteAsync(leaf, new[] { leaf });

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_with_children_is_blocked()
    {
        var parent = Cat(Guid.NewGuid());
        var child = Cat(Guid.NewGuid(), parent.Id);

        var result = await _service.DeleteAsync(parent, new[] { parent, child });

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(CategoryService.InUseCode);
    }

    [Fact]
    public async Task Delete_referenced_by_a_transaction_is_blocked()
    {
        var leaf = Cat(Guid.NewGuid());
        _references.IsReferencedAsync(leaf.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.DeleteAsync(leaf, new[] { leaf });

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(CategoryService.InUseCode);
    }
}
