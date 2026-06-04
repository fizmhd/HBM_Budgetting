using Microsoft.Extensions.Options;
using BudgetTracker.Api.Infrastructure.Options;

namespace BudgetTracker.Api.Infrastructure.Security;

/// <summary>
/// Helper class for validating password strength
/// </summary>
public class PasswordValidator
{
    private readonly PasswordOptions _options;

    public PasswordValidator(IOptions<PasswordOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Validates a password against configured password policy
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>List of validation error messages (empty if valid)</returns>
    public List<string> Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required");
            return errors;
        }

        // Check minimum length
        if (password.Length < _options.MinimumLength)
        {
            errors.Add($"Password must be at least {_options.MinimumLength} characters long");
        }

        // Check for uppercase letter
        if (_options.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter");
        }

        // Check for lowercase letter
        if (_options.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter");
        }

        // Check for digit
        if (_options.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit");
        }

        // Check for non-alphanumeric character (special character)
        if (_options.RequireNonAlphanumeric && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("Password must contain at least one special character");
        }

        // Check for unique characters
        if (_options.RequiredUniqueChars > 0)
        {
            var uniqueChars = password.Distinct().Count();
            if (uniqueChars < _options.RequiredUniqueChars)
            {
                errors.Add($"Password must contain at least {_options.RequiredUniqueChars} unique characters");
            }
        }

        return errors;
    }

    /// <summary>
    /// Checks if a password is valid
    /// </summary>
    /// <param name="password">The password to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid(string password)
    {
        return Validate(password).Count == 0;
    }
}
