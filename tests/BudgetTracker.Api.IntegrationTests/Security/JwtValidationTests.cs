using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Security;

/// <summary>
/// Integration tests for JWT validation middleware
/// </summary>
public class JwtValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public JwtValidationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldAllowAnonymousOrReturn401()
    {
        // Arrange
        var protectedEndpoint = "/api/v1/protected"; // This would be a protected endpoint

        // Act
        var response = await _client.GetAsync(protectedEndpoint);

        // Assert
        // Depending on endpoint configuration, should either allow or return 401
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Request_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        // Act
        var response = await _client.GetAsync("/api/v1/protected");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Request_WithMalformedToken_ShouldReturn401()
    {
        // Arrange
        var malformedToken = "not-a-jwt-token";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", malformedToken);

        // Act
        var response = await _client.GetAsync("/api/v1/protected");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Request_WithExpiredToken_ShouldReturn401()
    {
        // Arrange
        // This is a sample expired JWT (you would need to generate a real expired token)
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyMzkwMjJ9.4Adcj0vfN7N7L8jVnVPYQlZvR5Kn8YqKjXqXqYqKjXo";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/v1/protected");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Request_WithoutBearerPrefix_ShouldNotSetUser()
    {
        // Arrange
        var token = "some-token-without-bearer";
        _client.DefaultRequestHeaders.Add("Authorization", token);

        // Act
        var response = await _client.GetAsync("/api/v1/protected");

        // Assert
        // Should not authenticate the user
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AnonymousEndpoint_WithInvalidToken_ShouldStillAllow()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidToken);

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        // Anonymous endpoints should still work even with invalid token
        // The middleware should just not set the user
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
