using BudgetTracker.Api.Infrastructure.Authentication;
using BudgetTracker.Shared.Results;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;

namespace BudgetTracker.Api.Infrastructure.Authentication;

/// <summary>
/// Supabase implementation of IAuthProvider
/// </summary>
public class SupabaseAuthProvider : IAuthProvider
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<SupabaseAuthProvider> _logger;

    public SupabaseAuthProvider(
        Supabase.Client supabaseClient,
        ILogger<SupabaseAuthProvider> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<Result<AuthProviderResponse>> RegisterAsync(string email, string password)
    {
        try
        {
            var session = await _supabaseClient.Auth.SignUp(email, password);

            if (session?.User == null)
            {
                return Error.Internal("AUTH_PROVIDER_ERROR", "Failed to create user account");
            }

            return MapToAuthProviderResponse(session);
        }
        catch (GotrueException ex) when (ex.Message.Contains("already registered"))
        {
            _logger.LogWarning(ex, "Registration failed: User already exists for email {Email}", email);
            return Error.Conflict("USER_ALREADY_EXISTS", "A user with this email already exists");
        }
        catch (GotrueException ex)
        {
            _logger.LogError(ex, "Supabase registration error for email {Email}", email);
            return Error.Internal("AUTH_PROVIDER_ERROR", $"Registration failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for email {Email}", email);
            return Error.Internal("UNEXPECTED_ERROR", "An unexpected error occurred during registration");
        }
    }

    public async Task<Result<AuthProviderResponse>> LoginAsync(string email, string password)
    {
        try
        {
            var session = await _supabaseClient.Auth.SignIn(email, password);

            if (session?.User == null)
            {
                return Error.Unauthorized("INVALID_CREDENTIALS", "Invalid email or password");
            }

            return MapToAuthProviderResponse(session);
        }
        catch (GotrueException ex) when (ex.Message.Contains("Invalid login credentials"))
        {
            _logger.LogWarning("Login failed: Invalid credentials for email {Email}", email);
            return Error.Unauthorized("INVALID_CREDENTIALS", "Invalid email or password");
        }
        catch (GotrueException ex) when (ex.Message.Contains("Email not confirmed"))
        {
            _logger.LogWarning("Login failed: Email not confirmed for {Email}", email);
            return Error.Unauthorized("EMAIL_NOT_CONFIRMED", "Please confirm your email before logging in");
        }
        catch (GotrueException ex)
        {
            _logger.LogError(ex, "Supabase login error for email {Email}", email);
            return Error.Internal("AUTH_PROVIDER_ERROR", $"Login failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for email {Email}", email);
            return Error.Internal("UNEXPECTED_ERROR", "An unexpected error occurred during login");
        }
    }

    public async Task<Result> LogoutAsync(string accessToken)
    {
        try
        {
            await _supabaseClient.Auth.SignOut();
            return Result.Success();
        }
        catch (GotrueException ex)
        {
            _logger.LogError(ex, "Supabase logout error");
            return Result.Failure(Error.Internal("AUTH_PROVIDER_ERROR", $"Logout failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout");
            return Result.Failure(Error.Internal("UNEXPECTED_ERROR", "An unexpected error occurred during logout"));
        }
    }

    public async Task<Result<AuthProviderResponse>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var session = await _supabaseClient.Auth.RefreshSession();

            if (session?.User == null)
            {
                return Error.Unauthorized("INVALID_TOKEN", "Invalid or expired refresh token");
            }

            return MapToAuthProviderResponse(session);
        }
        catch (GotrueException ex) when (ex.Message.Contains("Invalid Refresh Token"))
        {
            _logger.LogWarning("Token refresh failed: Invalid refresh token");
            return Error.Unauthorized("INVALID_TOKEN", "Invalid or expired refresh token");
        }
        catch (GotrueException ex)
        {
            _logger.LogError(ex, "Supabase token refresh error");
            return Error.Internal("AUTH_PROVIDER_ERROR", $"Token refresh failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return Error.Internal("UNEXPECTED_ERROR", "An unexpected error occurred during token refresh");
        }
    }

    public async Task<Result> ForgotPasswordAsync(string email)
    {
        try
        {
            await _supabaseClient.Auth.ResetPasswordForEmail(email);
            return Result.Success();
        }
        catch (GotrueException ex)
        {
            _logger.LogError(ex, "Supabase forgot password error for email {Email}", email);
            return Result.Failure(Error.Internal("AUTH_PROVIDER_ERROR", $"Password reset request failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during forgot password for email {Email}", email);
            return Result.Failure(Error.Internal("UNEXPECTED_ERROR", "An unexpected error occurred during password reset request"));
        }
    }

    public async Task<Result> ResetPasswordAsync(string token, string newPassword)
    {
        try
        {
            // Note: Supabase handles password reset via email link
            // This method would be called after the user clicks the link
            var attributes = new UserAttributes { Password = newPassword };
            await _supabaseClient.Auth.Update(attributes);
            return Result.Success();
        }
        catch (GotrueException ex) when (ex.Message.Contains("Invalid token"))
        {
            _logger.LogWarning("Password reset failed: Invalid token");
            return Result.Failure(Error.Unauthorized("INVALID_TOKEN", "Invalid or expired password reset token"));
        }
        catch (GotrueException ex)
        {
            _logger.LogError(ex, "Supabase password reset error");
            return Result.Failure(Error.Internal("AUTH_PROVIDER_ERROR", $"Password reset failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during password reset");
            return Result.Failure(Error.Internal("UNEXPECTED_ERROR", "An unexpected error occurred during password reset"));
        }
    }

    public async Task<Result> ConfirmEmailAsync(string token)
    {
        try
        {
            // Supabase handles email confirmation via link automatically
            // This is a placeholder for explicit confirmation if needed
            await Task.CompletedTask;
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during email confirmation");
            return Result.Failure(Error.Internal("UNEXPECTED_ERROR", "An unexpected error occurred during email confirmation"));
        }
    }

    private AuthProviderResponse MapToAuthProviderResponse(Session session)
    {
        return new AuthProviderResponse
        {
            ExternalUserId = session.User!.Id ?? string.Empty,
            Email = session.User.Email ?? string.Empty,
            AccessToken = session.AccessToken ?? string.Empty,
            RefreshToken = session.RefreshToken ?? string.Empty,
            ExpiresAt = session.ExpiresAt(),
            EmailConfirmed = session.User.EmailConfirmedAt.HasValue
        };
    }
}
