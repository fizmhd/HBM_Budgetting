using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Categories;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Categories.UpdateCategory;

/// <summary>
/// Renames a category and updates its icon / sort order. Renaming is always allowed (TASK 3.2).
/// </summary>
public class UpdateCategoryEndpoint : Endpoint<UpdateCategoryRequest, CategoryDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICategoryRepository _categories;
    private readonly IHouseholdMemberRepository _members;
    private readonly ICategoryService _categoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public UpdateCategoryEndpoint(
        ICurrentUserService currentUser,
        ICategoryRepository categories,
        IHouseholdMemberRepository members,
        ICategoryService categoryService,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _categories = categories;
        _members = members;
        _categoryService = categoryService;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Put("/api/v1/categories/{id}");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(UpdateCategoryRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var category = await _categories.GetByIdAsync(Route<Guid>("id"), ct);
        if (category is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        if (!category.IsVisibleTo(userId.Value, membership?.HouseholdId))
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var rename = _categoryService.Rename(category, req.Name);
        if (rename.IsFailure)
        {
            ThrowError(rename.Errors[0].Message, 400);
            return;
        }

        category.Icon = string.IsNullOrWhiteSpace(req.Icon) ? null : req.Icon.Trim();
        category.SortOrder = req.SortOrder;

        _categories.Update(category);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(category.ToDto(), ct);
    }
}
