using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Categories;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Categories.SeedDefaultCategories;

/// <summary>
/// Imports the default Excel/Swedish taxonomy into the caller's scope (TASK 3.6). Idempotent: does
/// nothing if the caller already has categories. Returns the resulting tree.
/// </summary>
public class SeedDefaultCategoriesEndpoint : EndpointWithoutRequest<List<CategoryTreeNodeDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICategoryRepository _categories;
    private readonly IHouseholdMemberRepository _members;
    private readonly ICategorySeeder _seeder;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public SeedDefaultCategoriesEndpoint(
        ICurrentUserService currentUser,
        ICategoryRepository categories,
        IHouseholdMemberRepository members,
        ICategorySeeder seeder,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _categories = categories;
        _members = members;
        _seeder = seeder;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/categories/seed-defaults");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 10, durationSeconds: 60);
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

        // Only seed when the scope is empty, so re-invoking never duplicates the tree.
        if (!await _categories.HasAnyAsync(userId.Value, membership?.HouseholdId, ct))
        {
            var defaults = _seeder.BuildDefaults(userId.Value, membership?.HouseholdId);
            foreach (var category in defaults)
            {
                await _categories.AddAsync(category, ct);
            }
            await _unitOfWork.SaveChangesAsync(ct);
        }

        var categories = await _categories.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct);
        await SendOkAsync(CategoryMapping.BuildTree(categories), ct);
    }
}
