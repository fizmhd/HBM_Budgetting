using BudgetTracker.Api.Infrastructure.Authentication;

namespace BudgetTracker.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware for validating JWT tokens from incoming requests
/// </summary>
public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(
        RequestDelegate next,
        ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenValidator tokenValidator)
    {
        // Extract token from Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            _logger.LogDebug("Bearer token received");

            if (!string.IsNullOrEmpty(token))
            {
                // Validate the token and get ClaimsPrincipal
                var validationResult = await tokenValidator.ValidateAndGetPrincipalAsync(token);

                if (validationResult.IsSuccess)
                {
                    // Set the claims principal in HttpContext
                    context.User = validationResult.Value;
                    _logger.LogDebug("JWT validated successfully for user {UserId}", 
                        context.User.FindFirst("sub")?.Value);
                }
                else
                {
                    _logger.LogWarning("JWT validation failed. Errors: {Errors}",
                        string.Join(", ", validationResult.Errors.Select(e => e.Message)));
                    
                    // Don't return 401 here - let the endpoint decide if authentication is required
                    // If the endpoint has [AllowAnonymous], it should proceed.
                    // If it requires [Authorize], the framework will return 401/403.
                }
            }
        }

        await _next(context);
    }
}
