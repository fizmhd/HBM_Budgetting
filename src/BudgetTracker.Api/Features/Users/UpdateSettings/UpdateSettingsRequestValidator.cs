using FastEndpoints;
using FluentValidation;
using BudgetTracker.Shared.DTOs.Users;

namespace BudgetTracker.Api.Features.Users.UpdateSettings;

/// <summary>
/// Validator for update settings requests
/// </summary>
public class UpdateSettingsRequestValidator : Validator<UpdateSettingsRequest>
{
    private static readonly string[] AllowedCurrencies = { "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "SEK", "NZD" };
    private static readonly string[] AllowedDateFormats = { "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy" };
    private static readonly string[] AllowedThemes = { "light", "dark", "system" };

    public UpdateSettingsRequestValidator()
    {
        When(x => !string.IsNullOrEmpty(x.PreferredCurrency), () =>
        {
            RuleFor(x => x.PreferredCurrency)
                .Must(x => AllowedCurrencies.Contains(x))
                .WithMessage($"Currency must be one of: {string.Join(", ", AllowedCurrencies)}");
        });

        When(x => !string.IsNullOrEmpty(x.DateFormat), () =>
        {
            RuleFor(x => x.DateFormat)
                .Must(x => AllowedDateFormats.Contains(x))
                .WithMessage($"Date format must be one of: {string.Join(", ", AllowedDateFormats)}");
        });

        When(x => !string.IsNullOrEmpty(x.Theme), () =>
        {
            RuleFor(x => x.Theme)
                .Must(x => AllowedThemes.Contains(x))
                .WithMessage($"Theme must be one of: {string.Join(", ", AllowedThemes)}");
        });
    }
}
