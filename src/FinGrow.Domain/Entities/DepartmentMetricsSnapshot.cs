namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public sealed class DepartmentMetricsSnapshot : AggregateRoot
{
    private DepartmentMetricsSnapshot()
    {
    }

    private DepartmentMetricsSnapshot(
        Guid id,
        Guid companyId,
        Guid departmentId,
        MetricsPeriod period,
        PeriodMetrics metrics,
        DateTimeOffset computedAt)
        : base(id)
    {
        CompanyId = companyId;
        DepartmentId = departmentId;
        PeriodStart = period.Start;
        Metrics = metrics;
        ComputedAt = computedAt;
    }

    public Guid CompanyId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public DateOnly PeriodStart { get; private set; }

    public PeriodMetrics Metrics { get; private set; } = null!;

    public DateTimeOffset ComputedAt { get; private set; }

    public MetricsPeriod Period => MetricsPeriod.FromStart(PeriodStart);

    public static DepartmentMetricsSnapshot Take(
        Guid companyId,
        Guid departmentId,
        MetricsPeriod period,
        PeriodMetrics metrics,
        DateTimeOffset computedAt)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(metrics);

        if (companyId == Guid.Empty)
        {
            throw new DomainException("Una foto de metricas siempre pertenece a una empresa.");
        }

        if (departmentId == Guid.Empty)
        {
            throw new DomainException("Una foto de metricas de departamento siempre pertenece a un departamento.");
        }

        return new DepartmentMetricsSnapshot(Guid.CreateVersion7(), companyId, departmentId, period, metrics, computedAt);
    }

    public void Refresh(PeriodMetrics metrics, DateTimeOffset computedAt)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        Metrics = metrics;
        ComputedAt = computedAt;
    }
}
