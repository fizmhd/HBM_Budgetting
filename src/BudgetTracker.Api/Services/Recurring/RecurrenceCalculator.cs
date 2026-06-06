using BudgetTracker.Api.Infrastructure.Persistence.Entities;

namespace BudgetTracker.Api.Services.Recurring;

/// <summary>
/// Pure date arithmetic for recurring rules (TASK 5.1). Kept free of I/O so the next-due logic is
/// exhaustively unit-testable around month-length and leap-year edges.
/// </summary>
public static class RecurrenceCalculator
{
    /// <summary>
    /// The first date a rule is due: the start date, except for monthly rules with an explicit
    /// day-of-month, where it is that day on/after the start (rolling to the next month if the start is
    /// already past it).
    /// </summary>
    public static DateOnly FirstDueDate(DateOnly start, RecurrenceFrequency frequency, int? dayOfMonth)
    {
        if (frequency != RecurrenceFrequency.Monthly || dayOfMonth is not { } day)
        {
            return start;
        }

        var candidate = OnDayOfMonth(start.Year, start.Month, day);
        return candidate >= start ? candidate : OnDayOfMonth(start.AddMonths(1).Year, start.AddMonths(1).Month, day);
    }

    /// <summary>
    /// The next due date after <paramref name="current"/>, applying the frequency and interval. Monthly
    /// rules honour the rule's day-of-month (clamped to the target month's length); yearly/weekly/daily
    /// step by the interval.
    /// </summary>
    public static DateOnly Next(DateOnly current, RecurrenceFrequency frequency, int interval, int? dayOfMonth)
    {
        var step = Math.Max(1, interval);
        return frequency switch
        {
            RecurrenceFrequency.Daily => current.AddDays(step),
            RecurrenceFrequency.Weekly => current.AddDays(7 * step),
            RecurrenceFrequency.Monthly => NextMonthly(current, step, dayOfMonth),
            RecurrenceFrequency.Yearly => current.AddYears(step),
            _ => current.AddDays(step)
        };
    }

    private static DateOnly NextMonthly(DateOnly current, int step, int? dayOfMonth)
    {
        var target = current.AddMonths(step);
        var day = dayOfMonth ?? current.Day;
        return OnDayOfMonth(target.Year, target.Month, day);
    }

    /// <summary>A date for the given year/month on <paramref name="day"/>, clamped to the month length.</summary>
    private static DateOnly OnDayOfMonth(int year, int month, int day)
    {
        var clamped = Math.Clamp(day, 1, DateTime.DaysInMonth(year, month));
        return new DateOnly(year, month, clamped);
    }
}
