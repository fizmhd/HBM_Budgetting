using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Services.Payslips;

/// <summary>
/// Resolves the <see cref="ICountryPayslipProfile"/> for a country (TASK 8.2). The registry seam other
/// country profiles slot into.
/// </summary>
public interface ICountryPayslipProfileProvider
{
    /// <summary>Returns the profile for <paramref name="country"/>, or throws if none is registered.</summary>
    ICountryPayslipProfile Get(PayslipCountry country);

    /// <summary>True if a profile is registered for <paramref name="country"/>.</summary>
    bool Supports(PayslipCountry country);
}

/// <summary>
/// Default provider backed by the set of <see cref="ICountryPayslipProfile"/>s registered in DI.
/// </summary>
public sealed class CountryPayslipProfileProvider : ICountryPayslipProfileProvider
{
    private readonly IReadOnlyDictionary<PayslipCountry, ICountryPayslipProfile> _profiles;

    public CountryPayslipProfileProvider(IEnumerable<ICountryPayslipProfile> profiles)
    {
        _profiles = profiles.ToDictionary(p => p.Country);
    }

    public bool Supports(PayslipCountry country) => _profiles.ContainsKey(country);

    public ICountryPayslipProfile Get(PayslipCountry country)
    {
        if (_profiles.TryGetValue(country, out var profile))
        {
            return profile;
        }

        throw new NotSupportedException($"No payslip profile is registered for country '{country}'.");
    }
}
