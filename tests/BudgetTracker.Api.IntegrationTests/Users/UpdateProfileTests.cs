using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.DTOs.Users;
using BudgetTracker.Api.IntegrationTests.Endpoints; // For CustomWebApplicationFactory

namespace BudgetTracker.Api.IntegrationTests.Users;

public class UpdateProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UpdateProfileTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.ResetDatabase();
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        // Register a test user
        var registerRequest = new RegisterRequest
        {
            Email = $"testprofile{Guid.NewGuid():N}@example.com",
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

    [Fact]
    public async Task UpdateProfile_ShouldReturn401_WithoutAuthentication()
    {
        var request = new UpdateProfileRequest { FirstName = "Jane" };
        var response = await _client.PutAsJsonAsync("/api/v1/users/me", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturn400_WithTooLongName()
    {
        var client = await GetAuthenticatedClientAsync();
        var request = new UpdateProfileRequest { FirstName = new string('A', 101) };
        var response = await client.PutAsJsonAsync("/api/v1/users/me", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ShouldUpdateSuccessfully()
    {
        var client = await GetAuthenticatedClientAsync();
        var request = new UpdateProfileRequest 
        { 
            FirstName = "UpdatedFirst",
            LastName = "UpdatedLast"
        };

        var response = await client.PutAsJsonAsync("/api/v1/users/me", request);
        response.EnsureSuccessStatusCode();

        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(userDto);
        Assert.Equal("UpdatedFirst", userDto.FirstName);
        Assert.Equal("UpdatedLast", userDto.LastName);
    }
}
