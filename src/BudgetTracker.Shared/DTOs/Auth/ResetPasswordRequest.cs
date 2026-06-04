namespace BudgetTracker.Shared.DTOs.Auth;

/// <summary>
/// Request model for password reset
/// </summary>
public class ResetPasswordRequest
{
    /// <summary>
    /// Password reset token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// New password
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    public string ConfirmPassword { get; set; } = string.Empty;
}
