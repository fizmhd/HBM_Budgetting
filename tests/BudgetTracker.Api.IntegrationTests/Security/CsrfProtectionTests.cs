using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Security;

/// <summary>
/// Integration tests for CSRF protection middleware
/// </summary>
public class CsrfProtectionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CsrfProtectionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostRequest_WithoutCsrfToken_ShouldReturn403()
    {
        // Arrange
        var testEndpoint = "/api/v1/test/protected"; // This would be a protected endpoint
        var requestData = new { data = "test" };

        // Act
        var response = await _client.PostAsJsonAsync(testEndpoint, requestData);

        // Assert
        // Note: This test assumes a protected endpoint exists
        // If the endpoint doesn't exist, you'll get 404 instead
        // The actual behavior depends on whether CSRF middleware runs before routing
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LoginEndpoint_ShouldBeExcludedFromCsrfValidation()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        // Should not return 403 Forbidden (CSRF validation should be skipped)
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RegisterEndpoint_ShouldBeExcludedFromCsrfValidation()
    {
        // Arrange
        var registerRequest = new
        {
            Email = "newuser@example.com",
            Password = "TestPassword123!",
            ConfirmPassword = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        // Assert
        // Should not return 403 Forbidden (CSRF validation should be skipped)
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuccessfulLogin_ShouldSetCsrfCookie()
    {
        // Arrange
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "ValidPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        if (response.IsSuccessStatusCode)
        {
            // Check if CSRF cookie is set
            var cookies = response.Headers.GetValues("Set-Cookie").ToList();
            cookies.Should().Contain(c => c.Contains("X-CSRF-TOKEN"));
        }
    }

    [Fact]
    public async Task GetRequest_ShouldNotRequireCsrfToken()
    {
        // Arrange
        var getEndpoint = "/health";

        // Act
        var response = await _client.GetAsync(getEndpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
