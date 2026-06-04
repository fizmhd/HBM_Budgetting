namespace BudgetTracker.Shared.DTOs.Auth;

/// <summary>
/// Request model for forgot password
/// </summary>
public class ForgotPasswordRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;
}
