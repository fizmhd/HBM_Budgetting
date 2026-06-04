using BudgetTracker.Shared.Results;
using System.Security.Claims;

namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Interface for validating and extracting claims from JWT tokens
/// </summary>
public interface ITokenValidator
{
    /// <summary>
    /// Validates a JWT token and extracts claims
    /// </summary>
    Task<Result<TokenClaims>> ValidateTokenAsync(string token);

    /// <summary>
    /// Validates a JWT token and returns ClaimsPrincipal
    /// </summary>
    Task<Result<ClaimsPrincipal>> ValidateAndGetPrincipalAsync(string token);

    /// <summary>
    /// Extracts claims from a token without full validation
    /// </summary>
    TokenClaims? GetClaimsFromToken(string token);
}
