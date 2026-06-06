using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Shared.DTOs.Auth;
using BudgetTracker.Shared.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace BudgetTracker.Api.IntegrationTests.Infrastructure;

/// <summary>
/// A web-application factory whose external auth is fully stubbed, so the register → login → bearer
/// round-trip actually yields an authenticated, internally-resolved user. Used by feature tests that
/// need a real signed-in caller. The base <see cref="CustomWebApplicationFactory"/> deliberately
/// leaves auth unconfigured (documented limitation); this subclass fills that gap in isolation.
/// </summary>
public class AuthenticatedWebApplicationFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            // Configure the mocked auth provider to issue deterministic, round-trippable tokens.
            var authProvider = Substitute.For<IAuthProvider>();
            authProvider.RegisterAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(ci => Result<AuthProviderResponse>.Success(BuildResponse((string)ci[0])));
            authProvider.LoginAsync(Arg.Any<string>(), Arg.Any<string>())
                .Returns(ci => Result<AuthProviderResponse>.Success(BuildResponse((string)ci[0])));
            authProvider.LogoutAsync(Arg.Any<string>())
                .Returns(Result.Success());

            services.RemoveAll(typeof(IAuthProvider));
            services.AddSingleton(authProvider);

            // Validate those tokens locally instead of calling Supabase.
            services.RemoveAll(typeof(ITokenValidator));
            services.AddSingleton<ITokenValidator, FakeTokenValidator>();
        });
    }

    private static AuthProviderResponse BuildResponse(string email) => new()
    {
        ExternalUserId = email,
        Email = email,
        AccessToken = FakeTokenValidator.TokenPrefix + email,
        RefreshToken = Guid.NewGuid().ToString("N"),
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        EmailConfirmed = true
    };

    /// <summary>
    /// Registers and logs in a brand-new user, returning a client with the bearer token attached.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(string? email = null)
    {
        email ??= $"user-{Guid.NewGuid():N}@example.com";

        var register = new RegisterRequest
        {
            Email = email,
            Password = "Test@123456",
            ConfirmPassword = "Test@123456"
        };
        var client = CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register", register);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Email = email, Password = register.Password });
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }
}
