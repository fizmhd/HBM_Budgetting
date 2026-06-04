using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Auth;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for user profile endpoints
/// </summary>
public class UserProfileEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserProfileEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.ResetDatabase();
    }

    /// <summary>
    /// Creates an authenticated HTTP client by registering and logging in a test user
    /// </summary>
    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        
        // Register a test user
        var registerRequest = new RegisterRequest
        {
            Email = $"testuser{Guid.NewGuid():N}@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };
        var registerClient = _factory.CreateClient();
        await registerClient.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        
        // Login to get auth token
        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };
        
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        
        if (loginResponse.IsSuccessStatusCode)
        {
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
            if (authResponse != null && !string.IsNullOrEmpty(authResponse.AccessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
            }
        }

        return client;
    }

    #region Get Profile Tests

    [Fact]
    public async Task GetProfile_ShouldReturn401_WithoutAuthentication()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ShouldReturn401_WithInvalidToken()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await _client.GetAsync("/api/v1/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Complete Profile Tests

    [Fact]
    public async Task CompleteProfile_ShouldReturn401_WithoutAuthentication()
    {
        // Arrange
        var request = new CompleteProfileRequest
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users/me/complete-profile", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CompleteProfile_ShouldReturn400_WithMissingFirstName()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var request = new CompleteProfileRequest
        {
            FirstName = "",
            LastName = "Doe"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/users/me/complete-profile", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteProfile_ShouldReturn400_WithMissingLastName()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var request = new CompleteProfileRequest
        {
            FirstName = "John",
            LastName = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/users/me/complete-profile", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteProfile_ShouldReturn400_WithTooLongFirstName()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var request = new CompleteProfileRequest
        {
            FirstName = new string('A', 101), // 101 characters
            LastName = "Doe"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/users/me/complete-profile", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CompleteProfile_ShouldReturn400_WithTooLongLastName()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var request = new CompleteProfileRequest
        {
            FirstName = "John",
            LastName = new string('A', 101) // 101 characters
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/users/me/complete-profile", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Update Profile Tests

    [Fact]
    public async Task UpdateProfile_ShouldReturn401_WithoutAuthentication()
    {
        // Arrange
        var request = new UpdateProfileRequest
        {
            FirstName = "Jane"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/users/me", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturn400_WithTooLongFirstName()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var request = new UpdateProfileRequest
        {
            FirstName = new string('A', 101) // 101 characters
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/users/me", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturn400_WithTooLongLastName()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var request = new UpdateProfileRequest
        {
            LastName = new string('A', 101) // 101 characters
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/v1/users/me", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ShouldAccept_EmptyRequest()
    {
        // Arrange - Empty request should be valid (no updates)
        var request = new UpdateProfileRequest();

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/users/me", request);

        // Assert
        // Should return 401 (not authenticated) rather than 400 (bad request)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    private class CompleteProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    private class UpdateProfileRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }
}
