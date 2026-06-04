namespace BudgetTracker.Shared.DTOs.Auth;

/// <summary>
/// User data transfer object
/// </summary>
public class UserDto
{
    /// <summary>
    /// Internal user ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name (optional)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (optional)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Computed display name (FirstName LastName or email prefix)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User's preferred currency code
    /// </summary>
    public string PreferredCurrency { get; set; } = "USD";

    /// <summary>
    /// User's preferred date format
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// User's preferred theme
    /// </summary>
    public string Theme { get; set; } = "light";

    /// <summary>
    /// When the user joined
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Indicates whether the user has completed their profile
    /// </summary>
    public bool IsProfileComplete { get; set; }
}
