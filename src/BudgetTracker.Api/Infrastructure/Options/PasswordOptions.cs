namespace BudgetTracker.Api.Infrastructure.Options;

/// <summary>
/// Configuration options for password policy.
/// </summary>
public class PasswordOptions
{
    public const string SectionName = "Password";

    /// <summary>
    /// Gets or sets the minimum password length.
    /// </summary>
    public int MinimumLength { get; set; } = 8;

    /// <summary>
    /// Gets or sets whether to require a digit in the password.
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to require a lowercase character in the password.
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to require an uppercase character in the password.
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to require a non-alphanumeric character in the password.
    /// </summary>
    public bool RequireNonAlphanumeric { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum number of unique characters required.
    /// </summary>
    public int RequiredUniqueChars { get; set; } = 4;

    /// <summary>
    /// Gets or sets the password history count (prevent reuse).
    /// </summary>
    public int PasswordHistoryCount { get; set; } = 5;

    /// <summary>
    /// Gets or sets the password expiration days (0 = no expiration).
    /// </summary>
    public int ExpirationDays { get; set; } = 0;
}
