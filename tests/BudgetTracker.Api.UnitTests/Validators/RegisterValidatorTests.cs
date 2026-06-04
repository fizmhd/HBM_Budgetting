using BudgetTracker.Api.Features.Auth.Register;
using BudgetTracker.Api.Infrastructure.Options;
using BudgetTracker.Api.Infrastructure.Security;
using BudgetTracker.Shared.DTOs.Auth;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BudgetTracker.Api.UnitTests.Validators;

public class RegisterValidatorTests
{
    private readonly PasswordValidator _passwordValidator;
    private readonly RegisterRequestValidator _sut;

    public RegisterValidatorTests()
    {
        var options = Substitute.For<IOptions<PasswordOptions>>();
        options.Value.Returns(new PasswordOptions
        {
            MinimumLength = 8,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireNonAlphanumeric = true
        });

        _passwordValidator = new PasswordValidator(options);
        _sut = new RegisterRequestValidator(_passwordValidator);
    }

    [Fact]
    public void Validate_WithValidRequest_Passes()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        // Act & Assert
        var result = _sut.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithShortPassword_Fails()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Pass",
            ConfirmPassword = "Pass"
        };

        // Act & Assert
        var result = _sut.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters long");
    }

    [Fact]
    public void Validate_WithMismatchingPasswords_Fails()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123" // Missing !
        };

        // Act & Assert
        var result = _sut.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Passwords do not match");
    }
}
