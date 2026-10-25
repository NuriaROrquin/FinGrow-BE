namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.GetDepartmentMetrics;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Options;

public class GetDepartmentMetricsQueryHandlerTests
{
    private static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);
    private static readonly MetricsPeriod October = MetricsPeriod.Of(2026, 10);

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();

    private GetDepartmentMetricsQueryHandler CreateHandler() =>
        new(
            _scenario.CurrentUser,
            _scenario.Companies,
            _snapshots,
            new MetricsAnonymizationPolicy(Options.Create(new MetricsAnonymizationOptions())));

    [Fact]
    public async Task Hides_the_score_and_band_of_a_period_with_less_than_three_employees_with_score()
    {
        var department = _scenario.AddDepartment("Ventas");
        _snapshots.Add(DepartmentMetricsSnapshot.Take(
            _scenario.Company.Id,
            department.Id,
            September,
            PeriodMetrics.From(4, 3, 20, 2, 2, 0, 1, 2, 170),
            CompanyStaffScenario.CreatedAt));
        _snapshots.Add(DepartmentMetricsSnapshot.Take(
            _scenario.Company.Id,
            department.Id,
            October,
            PeriodMetrics.From(4, 3, 20, 2, 2, 0, 1, 3, 240),
            CompanyStaffScenario.Now));

        var result = await CreateHandler().Handle(
            new GetDepartmentMetricsQuery(department.Id),
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value[0].AverageFinancialHealthScore.ShouldBeNull();
        result.Value[0].HealthBand.ShouldBeNull();
        result.Value[0].ParticipationRate.ShouldBe(75m);
        result.Value[1].AverageFinancialHealthScore.ShouldBe(80m);
        result.Value[1].HealthBand.ShouldBe(FinancialHealthBand.Healthy);
    }
}
