namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics.Trends;

public class CompanyTrendsCalculatorTests
{
    [Fact]
    public void An_indicator_that_rises_at_least_two_points_is_a_positive_trend()
    {
        var trends = CompanyTrendsCalculator.Compare(
            Indicators((CompanyTrendIndicator.ParticipationRate, 50m)),
            Indicators((CompanyTrendIndicator.ParticipationRate, 52m)));

        var trend = trends.Positive.ShouldHaveSingleItem();
        trend.Indicator.ShouldBe(CompanyTrendIndicator.ParticipationRate);
        trend.Unit.ShouldBe(CompanyTrendUnit.Percentage);
        trend.PreviousValue.ShouldBe(50m);
        trend.CurrentValue.ShouldBe(52m);
        trend.Change.ShouldBe(2m);
        trends.Opportunities.ShouldBeEmpty();
    }

    [Fact]
    public void A_change_smaller_than_two_points_is_stable_and_not_reported()
    {
        var trends = CompanyTrendsCalculator.Compare(
            Indicators((CompanyTrendIndicator.ParticipationRate, 50m), (CompanyTrendIndicator.AverageFinancialHealthScore, 60m)),
            Indicators((CompanyTrendIndicator.ParticipationRate, 51.99m), (CompanyTrendIndicator.AverageFinancialHealthScore, 58.01m)));

        trends.Positive.ShouldBeEmpty();
        trends.Opportunities.ShouldBeEmpty();
    }

    [Fact]
    public void An_indicator_that_falls_is_an_opportunity()
    {
        var trends = CompanyTrendsCalculator.Compare(
            Indicators((CompanyTrendIndicator.GoalsAchieved, 6m)),
            Indicators((CompanyTrendIndicator.GoalsAchieved, 2m)));

        var trend = trends.Opportunities.ShouldHaveSingleItem();
        trend.Indicator.ShouldBe(CompanyTrendIndicator.GoalsAchieved);
        trend.Unit.ShouldBe(CompanyTrendUnit.Count);
        trend.Change.ShouldBe(-4m);
        trends.Positive.ShouldBeEmpty();
    }

    [Fact]
    public void A_concern_that_affects_fewer_employees_is_a_positive_trend_and_one_that_grows_is_an_opportunity()
    {
        var trends = CompanyTrendsCalculator.Compare(
            Indicators((CompanyTrendIndicator.LowSavings, 60m), (CompanyTrendIndicator.ThinCushion, 10m)),
            Indicators((CompanyTrendIndicator.LowSavings, 40m), (CompanyTrendIndicator.ThinCushion, 30m)));

        trends.Positive.ShouldHaveSingleItem().Indicator.ShouldBe(CompanyTrendIndicator.LowSavings);
        trends.Opportunities.ShouldHaveSingleItem().Indicator.ShouldBe(CompanyTrendIndicator.ThinCushion);
    }

    [Fact]
    public void An_indicator_missing_in_either_month_is_not_compared()
    {
        var trends = CompanyTrendsCalculator.Compare(
            Indicators((CompanyTrendIndicator.AverageFinancialHealthScore, 40m)),
            Indicators((CompanyTrendIndicator.ParticipationRate, 80m)));

        trends.Positive.ShouldBeEmpty();
        trends.Opportunities.ShouldBeEmpty();
    }

    [Fact]
    public void Trends_are_ordered_from_the_biggest_change_to_the_smallest()
    {
        var trends = CompanyTrendsCalculator.Compare(
            Indicators(
                (CompanyTrendIndicator.ParticipationRate, 50m),
                (CompanyTrendIndicator.AverageFinancialHealthScore, 50m),
                (CompanyTrendIndicator.CompletedCourses, 1m)),
            Indicators(
                (CompanyTrendIndicator.ParticipationRate, 53m),
                (CompanyTrendIndicator.AverageFinancialHealthScore, 60m),
                (CompanyTrendIndicator.CompletedCourses, 4m)));

        trends.Positive.Select(trend => trend.Indicator).ShouldBe(new[]
        {
            CompanyTrendIndicator.AverageFinancialHealthScore,
            CompanyTrendIndicator.ParticipationRate,
            CompanyTrendIndicator.CompletedCourses,
        });
    }

    [Fact]
    public void Each_indicator_has_its_unit()
    {
        CompanyTrendsCalculator.UnitOf(CompanyTrendIndicator.AverageFinancialHealthScore).ShouldBe(CompanyTrendUnit.Points);
        CompanyTrendsCalculator.UnitOf(CompanyTrendIndicator.CompletedCourses).ShouldBe(CompanyTrendUnit.Count);
        CompanyTrendsCalculator.UnitOf(CompanyTrendIndicator.GoalsAchieved).ShouldBe(CompanyTrendUnit.Count);
        CompanyTrendsCalculator.UnitOf(CompanyTrendIndicator.BudgetAdoption).ShouldBe(CompanyTrendUnit.Percentage);
        CompanyTrendsCalculator.UnitOf(CompanyTrendIndicator.MissedGoalContributions).ShouldBe(CompanyTrendUnit.Percentage);
    }

    private static Dictionary<CompanyTrendIndicator, decimal> Indicators(
        params (CompanyTrendIndicator Indicator, decimal Value)[] values) =>
        values.ToDictionary(value => value.Indicator, value => value.Value);
}
