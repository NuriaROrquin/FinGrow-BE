namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.GetCompanyDepartmentMetrics;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public class GetCompanyDepartmentMetricsQueryHandlerTests
{
    private static readonly MetricsPeriod October = MetricsPeriod.Of(2026, 10);

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();

    private GetCompanyDepartmentMetricsQueryHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _snapshots,
            new MetricsAnonymizationPolicy(Options.Create(new MetricsAnonymizationOptions())));

    [Fact]
    public async Task A_department_with_less_than_three_employees_with_score_keeps_its_metrics_without_score()
    {
        var department = _scenario.AddDepartment("Ventas");
        _snapshots.Add(DepartmentMetricsSnapshot.Take(
            _scenario.Company.Id,
            department.Id,
            October,
            PeriodMetrics.From(3, 3, 20, 2, 2, 0, 1, 1, 90),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(
            new GetCompanyDepartmentMetricsQuery(),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var departmentMetrics = result.Value.ShouldHaveSingleItem();
        departmentMetrics.IsMetricsHidden.ShouldBeFalse();
        var metrics = departmentMetrics.Metrics.ShouldHaveSingleItem();
        metrics.ParticipationRate.ShouldBe(100m);
        metrics.AverageFinancialHealthScore.ShouldBeNull();
        metrics.HealthBand.ShouldBeNull();
    }
}
