using BudgetTracker.Api.Services.Budgets;
using FluentAssertions;

namespace BudgetTracker.Api.UnitTests.Services;

/// <summary>
/// Unit tests for the spent-vs-budget computation (TASK 6.2), focused on the threshold and 100%
/// boundaries.
/// </summary>
public class BudgetCalculatorTests
{
    [Fact]
    public void Well_under_threshold_is_ok()
    {
        var p = BudgetCalculator.Evaluate(amount: 1000m, spent: 500m, alertThresholdPercent: 80);

        p.Spent.Should().Be(500m);
        p.Remaining.Should().Be(500m);
        p.PercentUsed.Should().Be(50m);
        p.Status.Should().Be(BudgetStatus.Ok);
    }

    [Fact]
    public void Exactly_at_threshold_is_warning()
    {
        var p = BudgetCalculator.Evaluate(amount: 1000m, spent: 800m, alertThresholdPercent: 80);

        p.PercentUsed.Should().Be(80m);
        p.Status.Should().Be(BudgetStatus.Warning);
    }

    [Fact]
    public void Just_below_threshold_is_still_ok()
    {
        var p = BudgetCalculator.Evaluate(amount: 1000m, spent: 799.99m, alertThresholdPercent: 80);

        p.Status.Should().Be(BudgetStatus.Ok);
    }

    [Fact]
    public void Between_threshold_and_limit_is_warning()
    {
        var p = BudgetCalculator.Evaluate(amount: 1000m, spent: 950m, alertThresholdPercent: 80);

        p.PercentUsed.Should().Be(95m);
        p.Status.Should().Be(BudgetStatus.Warning);
    }

    [Fact]
    public void Exactly_at_limit_is_exceeded()
    {
        var p = BudgetCalculator.Evaluate(amount: 1000m, spent: 1000m, alertThresholdPercent: 80);

        p.PercentUsed.Should().Be(100m);
        p.Remaining.Should().Be(0m);
        p.Status.Should().Be(BudgetStatus.Exceeded);
    }

    [Fact]
    public void Over_the_limit_is_exceeded_with_negative_remaining()
    {
        var p = BudgetCalculator.Evaluate(amount: 1000m, spent: 1250m, alertThresholdPercent: 80);

        p.PercentUsed.Should().Be(125m);
        p.Remaining.Should().Be(-250m);
        p.Status.Should().Be(BudgetStatus.Exceeded);
    }

    [Fact]
    public void No_spend_is_ok_at_zero_percent()
    {
        var p = BudgetCalculator.Evaluate(amount: 1000m, spent: 0m, alertThresholdPercent: 80);

        p.PercentUsed.Should().Be(0m);
        p.Status.Should().Be(BudgetStatus.Ok);
    }

    [Theory]
    [InlineData(BudgetStatus.Ok, 80, 0)]
    [InlineData(BudgetStatus.Warning, 80, 80)]
    [InlineData(BudgetStatus.Exceeded, 80, 100)]
    public void Alert_level_maps_status_to_threshold(BudgetStatus status, int threshold, int expected)
    {
        var progress = new BudgetProgress(0, 0, 0, status);
        BudgetCalculator.AlertLevel(progress, threshold).Should().Be(expected);
    }
}
