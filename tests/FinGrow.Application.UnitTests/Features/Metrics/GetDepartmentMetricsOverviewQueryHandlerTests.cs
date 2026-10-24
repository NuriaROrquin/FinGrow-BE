namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.GetDepartmentMetricsOverview;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public class GetDepartmentMetricsOverviewQueryHandlerTests
{
    private static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);
    private static readonly MetricsPeriod October = MetricsPeriod.Of(2026, 10);

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();

    private GetDepartmentMetricsOverviewQueryHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _snapshots,
            new MetricsAnonymizationPolicy(Options.Create(new MetricsAnonymizationOptions())));

    [Fact]
    public async Task Without_dates_returns_the_latest_snapshot_for_each_department()
    {
        var department = _scenario.AddDepartment("Ventas");
        _snapshots.Add(DepartmentMetricsSnapshot.Take(
            _scenario.Company.Id,
            department.Id,
            September,
            PeriodMetrics.From(3, 1, 10, 1, 1, 0, 1, 3, 180),
            CompanyStaffScenario.CreatedAt));
        _snapshots.Add(DepartmentMetricsSnapshot.Take(
            _scenario.Company.Id,
            department.Id,
            October,
            PeriodMetrics.From(3, 3, 15, 2, 2, 1, 2, 3, 240),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(
            new GetDepartmentMetricsOverviewQuery(),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var metrics = result.Value.Single();
        metrics.Period.ShouldBe(October.Start);
        metrics.Employees.ShouldBe(3);
        metrics.AverageScore.ShouldBe(80m);
        metrics.Participation.ShouldBe(100m);
        metrics.Status.ShouldBe(FinancialHealthBand.Healthy);
        metrics.IsMetricsHidden.ShouldBeFalse();
    }

    [Fact]
    public async Task Dates_select_the_latest_snapshot_inside_the_requested_range()
    {
        var department = _scenario.AddDepartment("Ventas");
        _snapshots.Add(DepartmentMetricsSnapshot.Take(
            _scenario.Company.Id,
            department.Id,
            September,
            PeriodMetrics.From(3, 1, 10, 1, 1, 0, 1, 3, 180),
            CompanyStaffScenario.CreatedAt));
        _snapshots.Add(DepartmentMetricsSnapshot.Take(
            _scenario.Company.Id,
            department.Id,
            October,
            PeriodMetrics.From(3, 3, 15, 2, 2, 1, 2, 3, 240),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(
            new GetDepartmentMetricsOverviewQuery(September.Start, September.Start),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var metrics = result.Value.Single();
        metrics.Period.ShouldBe(September.Start);
        metrics.AverageScore.ShouldBe(60m);
        metrics.Status.ShouldBe(FinancialHealthBand.Stable);
    }

    [Fact]
    public async Task A_department_with_less_than_three_employees_with_score_hides_only_its_score()
    {
        var department = _scenario.AddDepartment("Ventas");
        _snapshots.Add(DepartmentMetricsSnapshot.Take(
            _scenario.Company.Id,
            department.Id,
            October,
            PeriodMetrics.From(4, 2, 15, 2, 2, 1, 2, 2, 160),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(
            new GetDepartmentMetricsOverviewQuery(),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var metrics = result.Value.Single();
        metrics.Employees.ShouldBe(4);
        metrics.Participation.ShouldBe(50m);
        metrics.AverageScore.ShouldBeNull();
        metrics.Status.ShouldBeNull();
        metrics.IsMetricsHidden.ShouldBeFalse();
    }

    [Fact]
    public async Task A_department_below_the_minimum_hides_its_metrics()
    {
        var department = _scenario.AddDepartment("Marketing");
        _snapshots.Add(DepartmentMetricsSnapshot.Take(
            _scenario.Company.Id,
            department.Id,
            October,
            PeriodMetrics.From(2, 2, 10, 1, 1, 0, 1, 0, 0),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(
            new GetDepartmentMetricsOverviewQuery(),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var metrics = result.Value.Single();
        metrics.Employees.ShouldBeNull();
        metrics.AverageScore.ShouldBeNull();
        metrics.Participation.ShouldBeNull();
        metrics.Status.ShouldBeNull();
        metrics.IsMetricsHidden.ShouldBeTrue();
        metrics.MinimumGroupSize.ShouldBe(3);
        metrics.HiddenReason.ShouldNotBeNullOrEmpty();
    }
}
