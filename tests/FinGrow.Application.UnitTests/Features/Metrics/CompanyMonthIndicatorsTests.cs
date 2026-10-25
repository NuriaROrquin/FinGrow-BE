namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.Trends;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public class CompanyMonthIndicatorsTests
{
    private static readonly MetricsAnonymizationPolicy Policy =
        new(Options.Create(new MetricsAnonymizationOptions()));

    [Fact]
    public void Rates_are_computed_over_the_active_employees_and_counts_are_kept()
    {
        var indicators = CompanyMonthIndicators.Read(
            PeriodMetrics.From(8, 6, 80, 2, 4, 3, 1, 4, 240),
            Array.Empty<FinancialHealthFactors>(),
            completedCourses: 5,
            Policy);

        indicators[CompanyTrendIndicator.ParticipationRate].ShouldBe(75m);
        indicators[CompanyTrendIndicator.AverageFinancialHealthScore].ShouldBe(60m);
        indicators[CompanyTrendIndicator.BudgetAdoption].ShouldBe(25m);
        indicators[CompanyTrendIndicator.GoalAdoption].ShouldBe(50m);
        indicators[CompanyTrendIndicator.IntegrationAdoption].ShouldBe(12.5m);
        indicators[CompanyTrendIndicator.GoalsAchieved].ShouldBe(3m);
        indicators[CompanyTrendIndicator.CompletedCourses].ShouldBe(5m);
    }

    [Fact]
    public void A_company_below_the_threshold_reveals_nothing()
    {
        var indicators = CompanyMonthIndicators.Read(
            PeriodMetrics.From(2, 2, 20, 2, 2, 1, 2, 2, 120),
            new[] { LowSavings(), LowSavings() },
            completedCourses: 3,
            Policy);

        indicators.ShouldBeEmpty();
    }

    [Fact]
    public void The_average_score_is_hidden_when_few_employees_have_a_score()
    {
        var indicators = CompanyMonthIndicators.Read(
            PeriodMetrics.From(10, 10, 100, 0, 0, 0, 0, 2, 120),
            Array.Empty<FinancialHealthFactors>(),
            completedCourses: 0,
            Policy);

        indicators.ContainsKey(CompanyTrendIndicator.AverageFinancialHealthScore).ShouldBeFalse();
        indicators.ContainsKey(CompanyTrendIndicator.ParticipationRate).ShouldBeTrue();
    }

    [Fact]
    public void Concerns_are_hidden_when_few_employees_were_measured()
    {
        var indicators = CompanyMonthIndicators.Read(
            PeriodMetrics.From(10, 10, 100, 0, 0, 0, 0, 2, 40),
            new[] { LowSavings(), LowSavings() },
            completedCourses: 0,
            Policy);

        indicators.ContainsKey(CompanyTrendIndicator.LowSavings).ShouldBeFalse();
        indicators.ContainsKey(CompanyTrendIndicator.ThinCushion).ShouldBeFalse();
    }

    [Fact]
    public void A_concern_with_fewer_affected_than_the_threshold_is_hidden_but_one_with_none_is_shown_as_zero()
    {
        var indicators = CompanyMonthIndicators.Read(
            PeriodMetrics.From(5, 5, 50, 0, 0, 0, 0, 5, 400),
            new[] { LowSavings(), LowSavings(), Healthy(), Healthy(), Healthy() },
            completedCourses: 0,
            Policy);

        indicators.ContainsKey(CompanyTrendIndicator.LowSavings).ShouldBeFalse();
        indicators[CompanyTrendIndicator.ThinCushion].ShouldBe(0m);
        indicators.ContainsKey(CompanyTrendIndicator.BudgetOverruns).ShouldBeFalse();
        indicators.ContainsKey(CompanyTrendIndicator.MissedGoalContributions).ShouldBeFalse();
    }

    [Fact]
    public void A_concern_with_enough_affected_employees_is_revealed_as_a_percentage()
    {
        var indicators = CompanyMonthIndicators.Read(
            PeriodMetrics.From(4, 4, 40, 0, 0, 0, 0, 4, 200),
            new[] { LowSavings(), LowSavings(), LowSavings(), Healthy() },
            completedCourses: 0,
            Policy);

        indicators[CompanyTrendIndicator.LowSavings].ShouldBe(75m);
    }

    private static FinancialHealthFactors Healthy() => FinancialHealthFactors.From(100m, null, null, 100m);

    private static FinancialHealthFactors LowSavings() => FinancialHealthFactors.From(20m, null, null, 100m);
}
