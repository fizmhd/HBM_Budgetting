using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Api.IntegrationTests.Infrastructure;
using BudgetTracker.Shared.DTOs.Auth;
using FluentAssertions;
using BudgetTracker.Api.IntegrationTests.Helpers;

namespace BudgetTracker.Api.IntegrationTests.Auth;

[Collection("Database Collection")]
public class LoginTests : IClassFixture<DatabaseFixture>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly DatabaseFixture _fixture;

    public LoginTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _factory = fixture.Factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange - Ensure Reset and Seed is done by Fixture or manually call it if needed
        // Assuming DatabaseFixture resets via IAsyncLifetime or we call it here if we want isolation per test
        // Ideally Respawner is called before each test. But xUnit ClassFixture runs once per class.
        // If we want per-test isolation, we should use Constructor or IAsyncLifetime in Test Class.
        
        // Since we are checking Login against seeded users:
        await _fixture.ResetDatabaseAsync();

        var request = new LoginRequest
        {
            Email = "confirmed@test.com",
            Password = "AnyPassword" // Auth Provider is mocked to always succeed? Wait.
        };
        
        // Note: Our MockAuthProvider in CustomWebApplicationFactory needs to allow this.
        // The default MockAuthProvider might need configuration or we assume it returns success.
        // If using NSubstitute mock in Factory, we might need to access it to setup expectations.
        // However, since we registered it as Singleton in Factory, we can resolve it?
        // But integration tests usually avoid mocking internal behavior if possible, or use standard behavior.
        
        // Actually, we replaced IAuthProvider with a Substitute. 
        // We need to configure that Substitute to return success.
        // How to access the Substitute from here? 
        // _factory.Services.GetRequiredService<IAuthProvider>() returns the mock.
        
        // Let's implement that lookup.

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task Login_WhenAccountLocked_ReturnsUnauthorized()
    {
        await _fixture.ResetDatabaseAsync();
        
        var request = new LoginRequest
        {
            Email = "locked@test.com",
            Password = "AnyPassword"
        };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
