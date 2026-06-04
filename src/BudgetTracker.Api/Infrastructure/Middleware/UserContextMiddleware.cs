using BudgetTracker.Api.Infrastructure.Authentication;
using System.Security.Claims;

namespace BudgetTracker.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware for resolving and validating user context
/// Validates token version and stores user in HttpContext
/// </summary>
public class UserContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    public UserContextMiddleware(
        RequestDelegate next,
        ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IUserResolutionService userResolutionService)
    {
        // Only process if user is authenticated
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // Extract external user ID from JWT claims
            var externalUserId = context.User.FindFirst("sub")?.Value;
            var email = context.User.FindFirst("email")?.Value;
            var tokenVersionClaim = context.User.FindFirst("token_version")?.Value;

            if (!string.IsNullOrEmpty(externalUserId) && !string.IsNullOrEmpty(email))
            {
                // Resolve internal user
                var userResult = await userResolutionService.ResolveUserAsync(
                    AuthenticationConstants.SupabaseProviderName,
                    externalUserId,
                    email);

                if (userResult.IsSuccess)
                {
                    var user = userResult.Value;

                    // Validate token version
                    if (int.TryParse(tokenVersionClaim, out var tokenVersion))
                    {
                        if (user.TokenVersion != tokenVersion)
                        {
                            _logger.LogWarning(
                                "Token version mismatch for user {UserId}. Expected {Expected}, got {Actual}",
                                user.Id, user.TokenVersion, tokenVersion);

                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                error = "TOKEN_REVOKED",
                                message = "Token has been revoked. Please login again."
                            });
                            return;
                        }
                    }

                    // Store user in HttpContext.Items for CurrentUserService
                    context.Items[AuthenticationConstants.HttpContextUserItemKey] = user;

                    // Add internal user ID as a claim for easier access
                    var claims = new List<Claim>(context.User.Claims)
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, context.User.Identity?.AuthenticationType ?? "Bearer");
                    context.User = new ClaimsPrincipal(identity);

                    _logger.LogDebug("User context resolved for user {UserId}", user.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to resolve user for external ID {ExternalUserId}: {Errors}",
                        externalUserId, string.Join(", ", userResult.Errors.Select(e => e.Message)));

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "USER_RESOLUTION_FAILED",
                        message = "Failed to resolve user context"
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
