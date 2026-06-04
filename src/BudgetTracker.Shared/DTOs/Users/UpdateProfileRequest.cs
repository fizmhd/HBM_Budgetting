using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Shared.DTOs.Users;

/// <summary>
/// Request to update user profile
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// User's first name
    /// </summary>
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string? LastName { get; set; }
}
