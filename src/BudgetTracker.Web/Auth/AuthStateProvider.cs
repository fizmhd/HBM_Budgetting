using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BudgetTracker.Web.Auth;

public class AuthStateProvider : AuthenticationStateProvider
{
    private readonly TokenManager _tokenManager;
    private readonly IAuthService _authService;

    public AuthStateProvider(TokenManager tokenManager, IAuthService authService)
    {
        _tokenManager = tokenManager;
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!await _tokenManager.IsTokenValidAsync())
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var user = await _authService.GetCurrentUserAsync();
        if (user == null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("IsProfileComplete", user.IsProfileComplete.ToString())
        };

        if (!string.IsNullOrEmpty(user.FirstName))
        {
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        }

        if (!string.IsNullOrEmpty(user.LastName))
        {
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        var principal = new ClaimsPrincipal(identity);

        return new AuthenticationState(principal);
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
