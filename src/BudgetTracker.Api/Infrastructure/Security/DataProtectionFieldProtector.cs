using Microsoft.AspNetCore.DataProtection;

namespace BudgetTracker.Api.Infrastructure.Security;

/// <summary>
/// <see cref="IFieldProtector"/> backed by ASP.NET Core Data Protection. Encryption uses a dedicated,
/// versioned purpose string so payslip personnummer ciphertext can never be unprotected by a protector
/// created for any other purpose. The key ring is configured in <c>Program.cs</c> (persisted so values
/// stay decryptable across restarts).
/// </summary>
public sealed class DataProtectionFieldProtector : IFieldProtector
{
    /// <summary>Purpose string isolating personnummer ciphertext from any other protected data.</summary>
    public const string Purpose = "BudgetTracker.Payslip.Personnummer.v1";

    private readonly IDataProtector _protector;

    public DataProtectionFieldProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
