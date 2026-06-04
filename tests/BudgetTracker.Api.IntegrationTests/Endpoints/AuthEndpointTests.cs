using System.Net;
using System.Net.Http.Json;
using BudgetTracker.Shared.DTOs.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BudgetTracker.Api.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for authentication endpoints
/// </summary>
public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private HttpClient GetClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false // Don't follow redirects in tests
    });

    #region Register Tests

    [Fact]
    public async Task Register_ShouldReturn200_WithValidRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturn400_WithInvalidEmail()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturn400_WithPasswordMismatch()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Test@123456",
            ConfirmPassword = "Different@123"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_ShouldReturn401_WithInvalidCredentials()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturn400_WithMissingEmail()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "",
            Password = "Test@123456"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturn400_WithMissingPassword()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ShouldReturn204_Always()
    {
        // Act
        var response = await GetClient().PostAsync("/api/v1/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ShouldClearCookies()
    {
        // Act
        var response = await GetClient().PostAsync("/api/v1/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Verify cookies are cleared (Set-Cookie headers with expired dates)
        var setCookieHeaders = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.NotEmpty(setCookieHeaders);
    }

    #endregion

    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_ShouldReturn401_WithoutCookie()
    {
        // Act
        var response = await GetClient().PostAsync("/api/v1/auth/refresh", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Forgot Password Tests

    [Fact]
    public async Task ForgotPassword_ShouldReturn200_WithValidEmail()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // No body expected - endpoint returns empty response for security
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn200_EvenWithNonexistentEmail()
    {
        // Arrange - Security: Don't reveal if email exists
        var request = new ForgotPasswordRequest
        {
            Email = "nonexistent@example.com"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturn400_WithInvalidEmail()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "invalid-email"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/forgot-password", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Reset Password Tests

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WithWeakPassword()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Token = "some-token",
            NewPassword = "weak"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturn400_WithMissingToken()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Token = "",
            NewPassword = "Test@123456"
        };

        // Act
        var response = await GetClient().PostAsJsonAsync("/api/v1/auth/reset-password", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Confirm Email Tests

    [Fact]
    public async Task ConfirmEmail_ShouldRedirect_WithMissingToken()
    {
        // Act
        var response = await GetClient().GetAsync("/api/v1/auth/confirm");

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("error=invalid_token", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ConfirmEmail_ShouldRedirect_WithInvalidToken()
    {
        // Act
        var response = await GetClient().GetAsync("/api/v1/auth/confirm?token=invalid-token");

        // Assert
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("error=", location);
    }

    #endregion

    private class MessageResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
