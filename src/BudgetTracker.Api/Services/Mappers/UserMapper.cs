using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Shared.DTOs.Auth;

namespace BudgetTracker.Api.Services.Mappers;

/// <summary>
/// Mapper for User entity to UserDto conversions
/// </summary>
public class UserMapper
{
    /// <summary>
    /// Maps User entity to UserDto
    /// </summary>
    public UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = ComputeDisplayName(user),
            PreferredCurrency = user.PreferredCurrency,
            DateFormat = user.DateFormat,
            Theme = user.Theme,
            CreatedAt = user.CreatedAt,
            IsProfileComplete = user.IsProfileComplete
        };
    }

    /// <summary>
    /// Updates User entity from UserDto (for profile updates)
    /// </summary>
    public void UpdateFromDto(User user, UserDto dto)
    {
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.IsProfileComplete = dto.IsProfileComplete;
    }

    /// <summary>
    /// Computes display name from user data
    /// </summary>
    private string ComputeDisplayName(User user)
    {
        // If both first and last name exist, use "FirstName LastName"
        if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
        {
            return $"{user.FirstName} {user.LastName}";
        }

        // If only first name exists, use it
        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            return user.FirstName;
        }

        // If only last name exists, use it
        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            return user.LastName;
        }

        // Otherwise, use email prefix (before @)
        var atIndex = user.Email.IndexOf('@');
        return atIndex > 0 ? user.Email.Substring(0, atIndex) : user.Email;
    }
}
