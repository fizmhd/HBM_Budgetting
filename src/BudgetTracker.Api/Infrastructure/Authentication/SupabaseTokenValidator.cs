using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Api.Infrastructure.Options;
using BudgetTracker.Shared.Results;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Supabase implementation of ITokenValidator
/// </summary>
public class SupabaseTokenValidator : ITokenValidator
{
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseTokenValidator> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public SupabaseTokenValidator(
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseTokenValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
        _tokenHandler = new JwtSecurityTokenHandler();
        _tokenHandler.InboundClaimTypeMap.Clear(); // Don't map "sub" to ClaimTypes.NameIdentifier
    }

    public async Task<Result<TokenClaims>> ValidateTokenAsync(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret)),
                ValidateIssuer = false, // Supabase doesn't use issuer validation
                ValidateAudience = false, // Supabase doesn't use audience validation
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return Error.Unauthorized("INVALID_TOKEN", "Token is not a valid JWT");
            }

            var claims = ExtractClaims(jwtToken);
            if (claims == null)
            {
                return Error.Unauthorized("INVALID_TOKEN", "Token does not contain required claims");
            }

            await Task.CompletedTask;
            return Result<TokenClaims>.Success(claims);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token validation failed: Token expired");
            return Error.Unauthorized("TOKEN_EXPIRED", "Token has expired");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid token");
            return Error.Unauthorized("INVALID_TOKEN", "Token is invalid");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return Error.Internal("UNEXPECTED_ERROR", "An unexpected error occurred during token validation");
        }
    }

    public async Task<Result<ClaimsPrincipal>> ValidateAndGetPrincipalAsync(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret)),
                ValidateIssuer = false, // Supabase doesn't use issuer validation
                ValidateAudience = false, // Supabase doesn't use audience validation
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken)
            {
                return Error.Unauthorized("INVALID_TOKEN", "Token is not a valid JWT");
            }

            await Task.CompletedTask;
            return Result<ClaimsPrincipal>.Success(principal);
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("Token validation failed: Token expired");
            return Error.Unauthorized("TOKEN_EXPIRED", "Token has expired");
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: Invalid token. Message: {Message}", ex.Message);
            return Error.Unauthorized("INVALID_TOKEN", $"Token is invalid: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return Error.Internal("UNEXPECTED_ERROR", $"An unexpected error occurred during token validation: {ex.Message}");
        }
    }

    public TokenClaims? GetClaimsFromToken(string token)
    {
        try
        {
            var jwtToken = _tokenHandler.ReadJwtToken(token);
            return ExtractClaims(jwtToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract claims from token");
            return null;
        }
    }

    private TokenClaims? ExtractClaims(JwtSecurityToken jwtToken)
    {
        // Supabase uses "sub" for user ID and "email" for email
        var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        var email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == JwtRegisteredClaimNames.Email)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(email))
        {
            return null;
        }

        return new TokenClaims
        {
            ExternalUserId = userId,
            Email = email,
            ExpiresAt = jwtToken.ValidTo
        };
    }
}
