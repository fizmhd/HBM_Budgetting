using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Security;

/// <summary>
/// Integration tests for password validation
/// </summary>
public class PasswordValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PasswordValidationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("short", "Password must be at least 8 characters")]
    [InlineData("nouppercase123!", "uppercase")]
    [InlineData("NOLOWERCASE123!", "lowercase")]
    [InlineData("NoDigitsHere!", "digit")]
    [InlineData("NoSpecialChar123", "special")]
    public async Task Register_WithWeakPassword_ShouldReturnValidationError(string password, string expectedError)
    {
        // Arrange
        var registerRequest = new
        {
            Email = "weakpassword@example.com",
            Password = password,
            ConfirmPassword = password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().ContainAny(expectedError.ToLower(), expectedError);
    }

    [Fact]
    public async Task Register_WithStrongPassword_ShouldSucceed()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "strongpassword@example.com",
            Password = "StrongP@ssw0rd!",
            ConfirmPassword = "StrongP@ssw0rd!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        // Should not fail due to password validation
        // May fail for other reasons (e.g., user already exists, Supabase connection)
        response.StatusCode.Should().NotBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ShouldReturnValidationError()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "mismatch@example.com",
            Password = "StrongP@ssw0rd!",
            ConfirmPassword = "DifferentP@ssw0rd!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("match");
    }

    [Fact]
    public async Task Register_WithTooLongPassword_ShouldReturnValidationError()
    {
        // Arrange
        var longPassword = new string('A', 101) + "a1!"; // 104 characters
        var registerRequest = new
        {
            Email = "longpassword@example.com",
            Password = longPassword,
            ConfirmPassword = longPassword
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("100");
    }

    [Fact]
    public async Task Register_WithEmptyPassword_ShouldReturnValidationError()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "emptypassword@example.com",
            Password = "",
            ConfirmPassword = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("required");
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnValidationError()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "not-an-email",
            Password = "StrongP@ssw0rd!",
            ConfirmPassword = "StrongP@ssw0rd!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().ContainAny("email", "Email");
    }
}
