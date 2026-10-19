namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.GetCompanyMetricsComparison;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public class GetCompanyMetricsComparisonQueryHandlerTests
{
    private static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);
    private static readonly MetricsPeriod October = MetricsPeriod.Of(2026, 10);

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();

    private GetCompanyMetricsComparisonQueryHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _snapshots,
            new MetricsAnonymizationPolicy(Options.Create(new MetricsAnonymizationOptions())));

    [Fact]
    public async Task Returns_the_evolution_and_change_between_the_first_and_last_period()
    {
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _scenario.Company.Id,
            September,
            PeriodMetrics.From(5, 2, 10, 3, 2, 1, 2, 2, 140),
            CompanyStaffScenario.CreatedAt));
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _scenario.Company.Id,
            October,
            PeriodMetrics.From(5, 4, 12, 4, 3, 2, 3, 5, 375),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(
            new GetCompanyMetricsComparisonQuery(),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Periods.Select(period => period.Period).ShouldBe(new[] { September.Start, October.Start });
        result.Value.Periods[0].ParticipationRate.ShouldBe(40m);
        result.Value.Periods[1].ParticipationRate.ShouldBe(80m);
        result.Value.Periods[0].AverageFinancialHealthScore.ShouldBe(70m);
        result.Value.Periods[1].AverageFinancialHealthScore.ShouldBe(75m);
        result.Value.ParticipationRateChange.ShouldBe(40m);
        result.Value.AverageFinancialHealthScoreChange.ShouldBe(5m);
    }

    [Fact]
    public async Task Rejects_a_comparison_with_less_than_two_periods()
    {
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _scenario.Company.Id,
            September,
            PeriodMetrics.From(3, 1, 1, 0, 0, 0, 0, 0, 0),
            CompanyStaffScenario.CreatedAt));

        var result = await CreateHandler().Handle(
            new GetCompanyMetricsComparisonQuery(),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Metrics.NotEnoughPeriods");
    }
}
