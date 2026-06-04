using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Infrastructure.Persistence.Repositories;
using BudgetTracker.Api.Infrastructure.Authentication;
using System.Security.Claims;

namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Implementation of current user service
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public async Task<User?> GetUserAsync(CancellationToken cancellationToken = default)
    {
        if (!UserId.HasValue)
        {
            return null;
        }

        // Check if user is already cached in HttpContext.Items
        var httpContext = _httpContextAccessor.HttpContext;
        
        if (httpContext?.Items.TryGetValue(AuthenticationConstants.HttpContextUserItemKey, out var cachedUser) == true && cachedUser is User userObj)
        {
            return userObj;
        }

        // Load user from repository
        var loadedUser = await _userRepository.GetByIdAsync(UserId.Value, cancellationToken);
        
        // Cache in HttpContext.Items for the request lifetime
        if (loadedUser != null && httpContext != null)
        {
            httpContext.Items[AuthenticationConstants.HttpContextUserItemKey] = loadedUser;
        }

        return loadedUser;
    }
}
