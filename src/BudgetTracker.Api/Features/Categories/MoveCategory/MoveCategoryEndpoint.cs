using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using BudgetTracker.Shared.DTOs.Categories;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Categories.MoveCategory;

/// <summary>
/// Re-parents a category, preventing cycles (TASK 3.2).
/// </summary>
public class MoveCategoryEndpoint : Endpoint<MoveCategoryRequest, CategoryDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICategoryRepository _categories;
    private readonly IHouseholdMemberRepository _members;
    private readonly ICategoryService _categoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public MoveCategoryEndpoint(
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
        Put("/api/v1/categories/{id}/move");

        if (!_environment.IsEnvironment("Testing"))
        {
            Throttle(hitLimit: 60, durationSeconds: 60);
        }
    }

    public override async Task HandleAsync(MoveCategoryRequest req, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var membership = await _members.GetByUserIdAsync(userId.Value, ct);
        var scope = await _categories.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct);

        var category = scope.FirstOrDefault(c => c.Id == Route<Guid>("id"));
        if (category is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // A supplied new parent must also be visible to the caller.
        if (req.NewParentId is { } newParentId && scope.All(c => c.Id != newParentId))
        {
            ThrowError("Parent category not found.", 400);
            return;
        }

        var move = _categoryService.Move(category, req.NewParentId, scope);
        if (move.IsFailure)
        {
            ThrowError(move.Errors[0].Message, 400);
            return;
        }

        _categories.Update(category);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendOkAsync(category.ToDto(), ct);
    }
}
