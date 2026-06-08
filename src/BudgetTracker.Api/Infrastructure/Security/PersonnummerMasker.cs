namespace BudgetTracker.Api.Infrastructure.Security;

/// <summary>
/// Produces a display-safe mask of a Swedish personnummer (GDPR): the four-digit serial/checksum
/// suffix — the genuinely sensitive part — is hidden, leaving only the birth-date prefix. The mask is
/// computed once at write time and stored, so reads/lists never decrypt and the clear value is never
/// returned to the client.
/// </summary>
public static class PersonnummerMasker
{
    /// <summary>
    /// Masks <paramref name="personnummer"/> by replacing its last four digits with <c>****</c>
    /// (e.g. <c>"19900101-1234"</c> → <c>"19900101-****"</c>). Returns null for null/blank input.
    /// </summary>
    public static string? Mask(string? personnummer)
    {
        if (string.IsNullOrWhiteSpace(personnummer))
        {
            return null;
        }

        var trimmed = personnummer.Trim();
        var digitCount = trimmed.Count(char.IsDigit);

        // Too short to have a meaningful prefix to keep — hide it entirely.
        if (digitCount <= 4)
        {
            return "****";
        }

        // Walk back over the last four digits, masking each and preserving any separators after them.
        var chars = trimmed.ToCharArray();
        var masked = 0;
        for (var i = chars.Length - 1; i >= 0 && masked < 4; i--)
        {
            if (char.IsDigit(chars[i]))
            {
                chars[i] = '*';
                masked++;
            }
        }

        return new string(chars);
    }
}
