using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BudgetTracker.Api.IntegrationTests.Infrastructure;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string UserId = "test-user-id";
    public const string AuthenticationScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization))
        {
            return Task.FromResult(AuthenticateResult.Fail("No Authorization header"));
        }

        var authHeader = authorization.ToString();
        if (!authHeader.StartsWith(AuthenticationScheme + " ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization scheme"));
        }

        var token = authHeader.Substring(AuthenticationScheme.Length + 1).Trim();
        var parts = token.Split('|');
        var userId = parts.Length > 0 ? parts[0] : UserId;
        var email = parts.Length > 1 ? parts[1] : "test@example.com";
        var isProfileComplete = parts.Length > 2 && bool.Parse(parts[2]);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, email), // Mapping Email to Name for simplicity or specific logic
            new(ClaimTypes.Email, email),
            new("IsProfileComplete", isProfileComplete.ToString())
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
