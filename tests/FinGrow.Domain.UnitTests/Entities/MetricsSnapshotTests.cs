namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class MetricsSnapshotTests
{
    private static readonly DateTimeOffset ComputedAt = new(2026, 9, 1, 4, 0, 0, TimeSpan.Zero);
    private static readonly MetricsPeriod August = MetricsPeriod.Of(2026, 8);

    [Fact]
    public void A_company_snapshot_keeps_the_period_and_the_metrics()
    {
        var companyId = Guid.CreateVersion7();
        var metrics = PeriodMetrics.From(5, 3, 12, 2, 1, 0, 4);

        var snapshot = CompanyMetricsSnapshot.Take(companyId, August, metrics, ComputedAt);

        snapshot.CompanyId.ShouldBe(companyId);
        snapshot.PeriodStart.ShouldBe(new DateOnly(2026, 8, 1));
        snapshot.Period.ShouldBe(August);
        snapshot.Metrics.ShouldBe(metrics);
        snapshot.ComputedAt.ShouldBe(ComputedAt);
    }

    [Fact]
    public void Refreshing_replaces_the_metrics_and_the_computation_time()
    {
        var snapshot = CompanyMetricsSnapshot.Take(Guid.CreateVersion7(), August, PeriodMetrics.Empty, ComputedAt);
        var updated = PeriodMetrics.From(5, 4, 20, 2, 1, 0, 4);

        snapshot.Refresh(updated, ComputedAt.AddDays(2));

        snapshot.Metrics.ShouldBe(updated);
        snapshot.ComputedAt.ShouldBe(ComputedAt.AddDays(2));
        snapshot.PeriodStart.ShouldBe(new DateOnly(2026, 8, 1));
    }

    [Fact]
    public void A_department_snapshot_belongs_to_its_department_and_company()
    {
        var companyId = Guid.CreateVersion7();
        var departmentId = Guid.CreateVersion7();

        var snapshot = DepartmentMetricsSnapshot.Take(companyId, departmentId, August, PeriodMetrics.Empty, ComputedAt);

        snapshot.CompanyId.ShouldBe(companyId);
        snapshot.DepartmentId.ShouldBe(departmentId);
        snapshot.Period.ShouldBe(August);
    }

    [Fact]
    public void Snapshots_without_an_owner_are_rejected()
    {
        Should.Throw<DomainException>(() => CompanyMetricsSnapshot.Take(Guid.Empty, August, PeriodMetrics.Empty, ComputedAt));
        Should.Throw<DomainException>(() => DepartmentMetricsSnapshot.Take(Guid.Empty, Guid.CreateVersion7(), August, PeriodMetrics.Empty, ComputedAt));
        Should.Throw<DomainException>(() => DepartmentMetricsSnapshot.Take(Guid.CreateVersion7(), Guid.Empty, August, PeriodMetrics.Empty, ComputedAt));
    }
}
