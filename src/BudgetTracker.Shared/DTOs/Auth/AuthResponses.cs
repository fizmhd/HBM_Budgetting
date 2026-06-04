namespace BudgetTracker.Shared.DTOs.Auth;

/// <summary>
/// Response for login endpoint
/// </summary>
public class LoginResponse : AuthResponse
{
}

/// <summary>
/// Response for register endpoint
/// </summary>
public class RegisterResponse
{
    public string Message { get; set; } = "Registration successful. Please check your email to confirm your account.";
}

/// <summary>
/// Response for refresh token endpoint
/// </summary>
public class RefreshTokenResponse : AuthResponse
{
}
