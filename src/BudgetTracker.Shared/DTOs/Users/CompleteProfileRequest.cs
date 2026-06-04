using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Shared.DTOs.Users;

/// <summary>
/// Request to complete user profile during onboarding
/// </summary>
public class CompleteProfileRequest
{
    /// <summary>
    /// User's first name
    /// </summary>
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;
}
