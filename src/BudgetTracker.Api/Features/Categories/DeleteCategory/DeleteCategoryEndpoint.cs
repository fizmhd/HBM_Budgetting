using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Persistence;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Services.Interfaces;
using FastEndpoints;

namespace BudgetTracker.Api.Features.Categories.DeleteCategory;

/// <summary>
/// Deletes a category subject to the rules (TASK 3.2): blocked while it has children or is referenced
/// by a transaction split or budget, returning a friendly CATEGORY_IN_USE message.
/// </summary>
public class DeleteCategoryEndpoint : EndpointWithoutRequest
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICategoryRepository _categories;
    private readonly IHouseholdMemberRepository _members;
    private readonly ICategoryService _categoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _environment;

    public DeleteCategoryEndpoint(
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
        Delete("/api/v1/categories/{id}");

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
        var scope = await _categories.GetVisibleAsync(userId.Value, membership?.HouseholdId, ct);

        var category = scope.FirstOrDefault(c => c.Id == Route<Guid>("id"));
        if (category is null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        var delete = await _categoryService.DeleteAsync(category, scope, ct);
        if (delete.IsFailure)
        {
            // 409 Conflict — the category is in use; surface the friendly message.
            ThrowError(delete.Errors[0].Message, 409);
            return;
        }

        _categories.Delete(category);
        await _unitOfWork.SaveChangesAsync(ct);

        await SendNoContentAsync(ct);
    }
}
