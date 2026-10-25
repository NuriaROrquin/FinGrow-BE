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

    [Fact]
    public async Task Hides_only_the_period_when_active_employees_are_below_the_minimum()
    {
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _scenario.Company.Id,
            MetricsPeriod.Of(2026, 9),
            PeriodMetrics.From(2, 2, 30, 1, 1, 1, 1, 2, 140),
            CompanyStaffScenario.CreatedAt));
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _scenario.Company.Id,
            October,
            PeriodMetrics.From(5, 4, 30, 3, 2, 1, 2, 3, 219),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(
            new GetCompanyMetricsQuery(),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var hidden = result.Value[0];
        hidden.ActiveEmployees.ShouldBe(2);
        hidden.ParticipatingEmployees.ShouldBeNull();
        hidden.ParticipationRate.ShouldBeNull();
        hidden.ConfirmedTransactions.ShouldBeNull();
        hidden.AverageFinancialHealthScore.ShouldBeNull();
        hidden.FinancialHealthBand.ShouldBeNull();
        hidden.IsMetricsHidden.ShouldBeTrue();

        var visible = result.Value[1];
        visible.IsMetricsHidden.ShouldBeFalse();
        visible.ParticipatingEmployees.ShouldBe(4);
        visible.FinancialHealthBand.ShouldBe(FinGrow.Domain.Enums.FinancialHealthBand.Stable);
    }
}
