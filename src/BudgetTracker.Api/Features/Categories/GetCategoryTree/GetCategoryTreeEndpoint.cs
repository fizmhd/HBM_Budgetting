using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Categories;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Categories.GetCategoryTree;

/// <summary>
/// Returns the full category tree visible to the caller (own + household-shared), nested.
/// </summary>
public class GetCategoryTreeEndpoint : EndpointWithoutRequest<List<CategoryTreeNodeDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICategoryRepository _categories;
    private readonly IHouseholdMemberRepository _members;
    private readonly IWebHostEnvironment _environment;

    public GetCategoryTreeEndpoint(
        ICurrentUserService currentUser,
        ICategoryRepository categories,
        IHouseholdMemberRepository members,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _categories = categories;
        _members = members;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("/api/v1/categories/tree");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        var categories = await _categories.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct);

        await SendOkAsync(CategoryMapping.BuildTree(categories), ct);
    }
}
