using System.Net;
using System.Net.Http.Json;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for health check endpoints
/// </summary>
public class HealthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public HealthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LiveEndpoint_ShouldReturn200_WithHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(content);
        Assert.Equal("Healthy", content.Status);
        Assert.True(content.Timestamp > DateTime.MinValue);
    }

    [Fact]
    public async Task ReadyEndpoint_ShouldReturn200_WhenAllDependenciesHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<ReadyResponse>();
        Assert.NotNull(content);
        Assert.Equal("Healthy", content.OverallStatus);
        Assert.NotNull(content.Database);
        Assert.Equal("Healthy", content.Database.Status);
        Assert.NotNull(content.Supabase);
    }

    [Fact]
    public async Task ReadyEndpoint_ShouldIncludeTimestamp()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        var content = await response.Content.ReadFromJsonAsync<ReadyResponse>();
        Assert.NotNull(content);
        Assert.True(content.Timestamp > DateTime.MinValue);
        Assert.True(content.Timestamp <= DateTime.UtcNow);
    }

    private class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    private class ReadyResponse
    {
        public string OverallStatus { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public HealthCheckResult? Database { get; set; }
        public HealthCheckResult? Supabase { get; set; }
    }

    private class HealthCheckResult
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
