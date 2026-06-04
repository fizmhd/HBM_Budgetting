namespace BudgetTracker.Api.Infrastructure.Security;

/// <summary>
/// Service for generating and validating CSRF tokens
/// </summary>
public interface ICsrfService
{
    /// <summary>
    /// Generates a new CSRF token
    /// </summary>
    /// <returns>A cryptographically secure random token</returns>
    string GenerateToken();

    /// <summary>
    /// Validates that the provided token matches the expected token
    /// </summary>
    /// <param name="token">The token from the request header</param>
    /// <param name="expectedToken">The token from the cookie</param>
    /// <returns>True if tokens match, false otherwise</returns>
    bool ValidateToken(string token, string expectedToken);
}
