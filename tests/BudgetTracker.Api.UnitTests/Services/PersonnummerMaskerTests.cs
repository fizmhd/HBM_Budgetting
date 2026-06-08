using BudgetTracker.Api.Infrastructure.Security;
using FluentAssertions;

namespace BudgetTracker.Api.UnitTests.Services;

/// <summary>
/// Unit tests for the personnummer mask (TASK 8.1, GDPR): the four-digit serial/checksum suffix is
/// always hidden, the birth-date prefix and any separator are kept.
/// </summary>
public class PersonnummerMaskerTests
{
    [Theory]
    [InlineData("19900101-1234", "19900101-****")]
    [InlineData("199001011234", "19900101****")]
    [InlineData("900101-1234", "900101-****")]
    public void Masks_the_last_four_digits(string input, string expected)
    {
        PersonnummerMasker.Mask(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Returns_null_for_blank(string? input)
    {
        PersonnummerMasker.Mask(input).Should().BeNull();
    }

    [Fact]
    public void Short_values_are_fully_masked()
    {
        PersonnummerMasker.Mask("12").Should().Be("****");
    }
}
