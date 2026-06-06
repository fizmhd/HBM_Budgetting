using BudgetTracker.Api.Infrastructure.Persistence.Entities;
using BudgetTracker.Api.Services.Recurring;
using FluentAssertions;

namespace BudgetTracker.Api.UnitTests.Services;

/// <summary>
/// Unit tests for the recurrence date arithmetic (TASK 5.1), covering month-length and leap-year edges.
/// </summary>
public class RecurrenceCalculatorTests
{
    [Fact]
    public void Daily_advances_by_interval()
    {
        var next = RecurrenceCalculator.Next(new DateOnly(2026, 6, 1), RecurrenceFrequency.Daily, 3, null);
        next.Should().Be(new DateOnly(2026, 6, 4));
    }

    [Fact]
    public void Weekly_advances_by_weeks()
    {
        var next = RecurrenceCalculator.Next(new DateOnly(2026, 6, 1), RecurrenceFrequency.Weekly, 2, null);
        next.Should().Be(new DateOnly(2026, 6, 15));
    }

    [Fact]
    public void Monthly_keeps_day_when_no_explicit_day_of_month()
    {
        var next = RecurrenceCalculator.Next(new DateOnly(2026, 1, 15), RecurrenceFrequency.Monthly, 1, null);
        next.Should().Be(new DateOnly(2026, 2, 15));
    }

    [Fact]
    public void Monthly_clamps_day_of_month_to_short_months()
    {
        // Day 31 in January → clamps to 28 Feb (2026 is not a leap year).
        var next = RecurrenceCalculator.Next(new DateOnly(2026, 1, 31), RecurrenceFrequency.Monthly, 1, 31);
        next.Should().Be(new DateOnly(2026, 2, 28));
    }

    [Fact]
    public void Monthly_uses_day_of_month_on_following_month()
    {
        var next = RecurrenceCalculator.Next(new DateOnly(2026, 2, 28), RecurrenceFrequency.Monthly, 1, 31);
        next.Should().Be(new DateOnly(2026, 3, 31));
    }

    [Fact]
    public void Yearly_handles_leap_day()
    {
        var next = RecurrenceCalculator.Next(new DateOnly(2024, 2, 29), RecurrenceFrequency.Yearly, 1, null);
        next.Should().Be(new DateOnly(2025, 2, 28));
    }

    [Fact]
    public void First_due_is_start_when_not_monthly_with_day()
    {
        var first = RecurrenceCalculator.FirstDueDate(new DateOnly(2026, 6, 10), RecurrenceFrequency.Weekly, null);
        first.Should().Be(new DateOnly(2026, 6, 10));
    }

    [Fact]
    public void First_due_is_day_of_month_on_or_after_start()
    {
        // Start 10 June, day-of-month 15 → first due 15 June.
        RecurrenceCalculator.FirstDueDate(new DateOnly(2026, 6, 10), RecurrenceFrequency.Monthly, 15)
            .Should().Be(new DateOnly(2026, 6, 15));

        // Start 20 June, day-of-month 15 → first due rolls to 15 July.
        RecurrenceCalculator.FirstDueDate(new DateOnly(2026, 6, 20), RecurrenceFrequency.Monthly, 15)
            .Should().Be(new DateOnly(2026, 7, 15));
    }
}
