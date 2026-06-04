using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Shared.DTOs.Users;

/// <summary>
/// Request to update user settings
/// </summary>
public class UpdateSettingsRequest
{
    /// <summary>
    /// User's preferred currency code (e.g. USD, EUR)
    /// </summary>
    [StringLength(3, ErrorMessage = "Currency code cannot exceed 3 characters")]
    public string? PreferredCurrency { get; set; }

    /// <summary>
    /// User's preferred date format (e.g. yyyy-MM-dd)
    /// </summary>
    [StringLength(20, ErrorMessage = "Date format cannot exceed 20 characters")]
    public string? DateFormat { get; set; }

    /// <summary>
    /// User's preferred theme (light/dark)
    /// </summary>
    [StringLength(20, ErrorMessage = "Theme cannot exceed 20 characters")]
    public string? Theme { get; set; }
}
