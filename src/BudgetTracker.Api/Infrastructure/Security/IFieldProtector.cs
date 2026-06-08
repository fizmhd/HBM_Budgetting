namespace BudgetTracker.Api.Infrastructure.Security;

/// <summary>
/// Encrypts/decrypts a sensitive field for storage at rest (TASK 8.1, GDPR). Used for the personnummer
/// on a payslip. A seam over the concrete crypto so the storage format and key management can be
/// swapped without touching callers.
/// </summary>
public interface IFieldProtector
{
    /// <summary>Encrypts <paramref name="plaintext"/>, returning an opaque, persistable string.</summary>
    string Protect(string plaintext);

    /// <summary>Reverses <see cref="Protect"/>.</summary>
    string Unprotect(string protectedValue);
}
