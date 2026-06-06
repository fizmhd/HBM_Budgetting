using System.Security.Claims;
using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Shared.Results;

namespace BudgetTracker.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only token validator that round-trips the deterministic tokens issued by the mocked
/// <see cref="IAuthProvider"/> in <see cref="AuthenticatedWebApplicationFactory"/>. A token has the
/// form <c>test-token:{email}</c>; anything else is rejected so unauthenticated/invalid-token
/// behaviour still holds.
/// </summary>
public class FakeTokenValidator : ITokenValidator
{
    public const string TokenPrefix = "test-token:";

    public Task<Result<TokenClaims>> ValidateTokenAsync(string token)
    {
        var claims = GetClaimsFromToken(token);
        return Task.FromResult(claims is null
            ? Result<TokenClaims>.Failure(Error.Unauthorized("INVALID_TOKEN", "Invalid token"))
            : Result<TokenClaims>.Success(claims));
    }

    public Task<Result<ClaimsPrincipal>> ValidateAndGetPrincipalAsync(string token)
    {
        var email = ExtractEmail(token);
        if (email is null)
        {
            return Task.FromResult(Result<ClaimsPrincipal>.Failure(
                Error.Unauthorized("INVALID_TOKEN", "Invalid token")));
        }

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim("sub", email),
                new Claim("email", email),
                new Claim("token_version", "1")
            },
            authenticationType: "Bearer");

        return Task.FromResult(Result<ClaimsPrincipal>.Success(new ClaimsPrincipal(identity)));
    }

    public TokenClaims? GetClaimsFromToken(string token)
    {
        var email = ExtractEmail(token);
        return email is null
            ? null
            : new TokenClaims { ExternalUserId = email, Email = email, ExpiresAt = DateTime.UtcNow.AddHours(1) };
    }

    private static string? ExtractEmail(string token) =>
        !string.IsNullOrEmpty(token) && token.StartsWith(TokenPrefix, StringComparison.Ordinal)
            ? token[TokenPrefix.Length..]
            : null;
}
