using System.Security.Cryptography;

namespace BudgetTracker.Api.Infrastructure.Security;

/// <summary>
/// Implementation of CSRF token generation and validation
/// </summary>
public class CsrfService : ICsrfService
{
    /// <summary>
    /// Generates a cryptographically secure random CSRF token
    /// </summary>
    /// <returns>Base64-encoded random token</returns>
    public string GenerateToken()
    {
        var tokenBytes = new byte[32]; // 256 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        return Convert.ToBase64String(tokenBytes);
    }

    /// <summary>
    /// Validates CSRF token using constant-time comparison to prevent timing attacks
    /// </summary>
    /// <param name="token">Token from request header</param>
    /// <param name="expectedToken">Token from cookie</param>
    /// <returns>True if tokens match</returns>
    public bool ValidateToken(string token, string expectedToken)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expectedToken))
        {
            return false;
        }

        // Use constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(token),
            System.Text.Encoding.UTF8.GetBytes(expectedToken)
        );
    }
}
