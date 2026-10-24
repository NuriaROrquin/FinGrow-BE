namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.GetCompanyMetrics;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public class GetCompanyMetricsQueryHandlerTests
{
    private static readonly MetricsPeriod October = MetricsPeriod.Of(2026, 10);

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();

    private GetCompanyMetricsQueryHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _snapshots,
            new MetricsAnonymizationPolicy(Options.Create(new MetricsAnonymizationOptions())));

    [Fact]
    public async Task Shows_the_average_score_with_three_employees_with_score()
    {
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _scenario.Company.Id,
            October,
            PeriodMetrics.From(5, 4, 30, 3, 2, 1, 2, 3, 219),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(new GetCompanyMetricsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldHaveSingleItem().AverageFinancialHealthScore.ShouldBe(73m);
    }

    [Fact]
    public async Task Hides_only_the_average_score_with_less_than_three_employees_with_score()
    {
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _scenario.Company.Id,
            October,
            PeriodMetrics.From(5, 4, 30, 3, 2, 1, 2, 2, 150),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(new GetCompanyMetricsQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var metrics = result.Value.ShouldHaveSingleItem();
        metrics.AverageFinancialHealthScore.ShouldBeNull();
        metrics.ActiveEmployees.ShouldBe(5);
        metrics.ParticipationRate.ShouldBe(80m);
    }
}
