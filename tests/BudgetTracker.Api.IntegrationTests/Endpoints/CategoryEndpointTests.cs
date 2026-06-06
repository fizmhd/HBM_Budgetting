using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Categories;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for the Categories API (Sprint 3): tree, CRUD, move/cycle, and deletion rules.
/// </summary>
public class CategoryEndpointTests : IClassFixture<AuthenticatedWebApplicationFactory>
{
    private readonly AuthenticatedWebApplicationFactory _factory;

    public CategoryEndpointTests(AuthenticatedWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private static CreateCategoryRequest Cat(string name, Guid? parent = null) =>
        new() { Name = name, ParentCategoryId = parent };

    [Fact]
    public async Task Tree_requires_authentication()
    {
        var anonymous = _factory.CreateClient();
        var response = await anonymous.GetAsync("/api/v1/categories/tree");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_parent_and_child_then_tree_is_nested()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var parent = await (await client.PostAsJsonAsync("/api/v1/categories", Cat("Housing")))
            .Content.ReadFromJsonAsync<CategoryDto>();
        await client.PostAsJsonAsync("/api/v1/categories", Cat("Rent", parent!.Id));

        var tree = await client.GetFromJsonAsync<List<CategoryTreeNodeDto>>("/api/v1/categories/tree");

        tree.Should().ContainSingle();
        tree![0].Name.Should().Be("Housing");
        tree[0].Children.Should().ContainSingle().Which.Name.Should().Be("Rent");
    }

    [Fact]
    public async Task Seed_defaults_populates_the_taxonomy()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var tree = await (await client.PostAsync("/api/v1/categories/seed-defaults", null))
            .Content.ReadFromJsonAsync<List<CategoryTreeNodeDto>>();

        tree!.Should().Contain(n => n.Name == "Housing");
        tree.Should().Contain(n => n.Name == "Subscriptions");
        // Housing has sub-items.
        tree.First(n => n.Name == "Housing").Children.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Delete_with_children_is_blocked_with_category_in_use()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var parent = await (await client.PostAsJsonAsync("/api/v1/categories", Cat("Transport")))
            .Content.ReadFromJsonAsync<CategoryDto>();
        await client.PostAsJsonAsync("/api/v1/categories", Cat("Fuel", parent!.Id));

        var response = await client.DeleteAsync($"/api/v1/categories/{parent.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Leaf_delete_succeeds()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var leaf = await (await client.PostAsJsonAsync("/api/v1/categories", Cat("Plants")))
            .Content.ReadFromJsonAsync<CategoryDto>();

        var response = await client.DeleteAsync($"/api/v1/categories/{leaf!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Move_under_own_descendant_is_rejected()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var root = await (await client.PostAsJsonAsync("/api/v1/categories", Cat("Root")))
            .Content.ReadFromJsonAsync<CategoryDto>();
        var child = await (await client.PostAsJsonAsync("/api/v1/categories", Cat("Child", root!.Id)))
            .Content.ReadFromJsonAsync<CategoryDto>();

        var response = await client.PutAsJsonAsync($"/api/v1/categories/{root.Id}/move",
            new MoveCategoryRequest { NewParentId = child!.Id });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Rename_is_allowed()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var cat = await (await client.PostAsJsonAsync("/api/v1/categories", Cat("Groce")))
            .Content.ReadFromJsonAsync<CategoryDto>();

        var updated = await (await client.PutAsJsonAsync($"/api/v1/categories/{cat!.Id}",
            new UpdateCategoryRequest { Name = "Groceries" })).Content.ReadFromJsonAsync<CategoryDto>();

        updated!.Name.Should().Be("Groceries");
    }
}
