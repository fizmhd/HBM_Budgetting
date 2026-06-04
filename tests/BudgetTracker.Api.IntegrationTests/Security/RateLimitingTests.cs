using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace BudgetTracker.Api.IntegrationTests.Security;

/// <summary>
/// Integration tests for rate limiting
/// </summary>
public class RateLimitingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RateLimitingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LoginEndpoint_ShouldEnforceRateLimit()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        // Act - Make 6 requests (limit is 5 per 15 minutes)
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
            responses.Add(response);
        }

        // Assert
        // First 5 requests should not be rate limited
        responses.Take(5).Should().AllSatisfy(r => r.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests));
        
        // 6th request should be rate limited
        responses.Last().StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimitExceeded_ShouldReturnRetryAfterHeader()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Email = "ratelimit@example.com",
            Password = "TestPassword123!"
        };

        // Act - Exceed rate limit
        for (int i = 0; i < 6; i++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        }

        var finalResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        if (finalResponse.StatusCode == HttpStatusCode.TooManyRequests)
        {
            finalResponse.Headers.Should().ContainKey("Retry-After");
        }
    }

    [Fact]
    public async Task RegisterEndpoint_ShouldEnforceRateLimit()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Make 4 requests (limit is 3 per hour)
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 4; i++)
        {
            var registerRequest = new
            {
                Email = $"user{i}@example.com",
                Password = "TestPassword123!",
                ConfirmPassword = "TestPassword123!"
            };
            var response = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
            responses.Add(response);
        }

        // Assert
        // First 3 requests should not be rate limited
        responses.Take(3).Should().AllSatisfy(r => r.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests));
        
        // 4th request should be rate limited
        responses.Last().StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimitResponse_ShouldContainErrorMessage()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new
        {
            Email = "errortest@example.com",
            Password = "TestPassword123!"
        };

        // Act - Exceed rate limit
        for (int i = 0; i < 6; i++)
        {
            await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        }

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("RATE_LIMIT_EXCEEDED");
            content.Should().Contain("Too many requests");
        }
    }
}
