using BudgetTracker.Shared.DTOs.Categories;
using FastEndpoints;
using FluentValidation;

namespace BudgetTracker.Api.Features.Categories.CreateCategory;

/// <summary>
/// Validator for creating a category.
/// </summary>
public class CreateCategoryRequestValidator : Validator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters");

        RuleFor(x => x.Icon)
            .MaximumLength(50).WithMessage("Icon cannot exceed 50 characters");
    }
}
