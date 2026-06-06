using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Shared.DTOs.Categories;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Categories.CreateCategory;

/// <summary>
/// Creates a category owned by the caller, optionally under a parent and/or shared with the household.
/// </summary>
public class CreateCategoryEndpoint : Endpoint<CreateCategoryRequest, CategoryDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICategoryRepository _categories;
    private readonly IHouseholdMemberRepository _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public CreateCategoryEndpoint(
        ICurrentUserService currentUser,
        ICategoryRepository categories,
        IHouseholdMemberRepository members,
        IUnitOfWork unitOfWork,
        IWebHostEnvironment environment)
    {
        _currentUser = currentUser;
        _categories = categories;
        _members = members;
        _unitOfWork = unitOfWork;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("/api/v1/categories");
        Validator<CreateCategoryRequestValidator>();

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(CreateCategoryRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);

        if (req.IsShared && membership is null)
        {
            ThrowError("You must belong to a household to share a category.", 400);
            return;
        }

        // A supplied parent must be visible to the caller.
        if (req.ParentCategoryId is { } parentId)
        {
            var parent = await _categories.GetByIdAsync(parentId, ct);
            if (parent is null || !parent.IsVisibleTo(userId.Value, membership?.HouseholdId))
            {
                ThrowError("Parent category not found.", 400);
                return;
            }
        }

        var category = new Category
        {
            OwnerUserId = userId.Value,
            Visibility = req.IsShared ? Visibility.HouseholdShared : Visibility.Individual,
            HouseholdId = req.IsShared ? membership!.HouseholdId : null,
            Name = req.Name.Trim(),
            ParentCategoryId = req.ParentCategoryId,
            Icon = string.IsNullOrWhiteSpace(req.Icon) ? null : req.Icon.Trim(),
            SortOrder = req.SortOrder,
            IsSystem = false
        };

        await _categories.AddAsync(category, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(category.ToDto(), ct);
    }
}
