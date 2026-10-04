namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public sealed class CompanyMetricsSnapshot : AggregateRoot
{
    private CompanyMetricsSnapshot()
    {
    }

    private CompanyMetricsSnapshot(
        Guid id,
        Guid companyId,
        MetricsPeriod period,
        PeriodMetrics metrics,
        DateTimeOffset computedAt)
        : base(id)
    {
        CompanyId = companyId;
        PeriodStart = period.Start;
        Metrics = metrics;
        ComputedAt = computedAt;
    }

    public Guid CompanyId { get; private set; }

    public DateOnly PeriodStart { get; private set; }

    public PeriodMetrics Metrics { get; private set; } = null!;

    public DateTimeOffset ComputedAt { get; private set; }

    public MetricsPeriod Period => MetricsPeriod.FromStart(PeriodStart);

    public static CompanyMetricsSnapshot Take(
        Guid companyId,
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

        return new CompanyMetricsSnapshot(Guid.CreateVersion7(), companyId, period, metrics, computedAt);
    }

    public void Refresh(PeriodMetrics metrics, DateTimeOffset computedAt)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        Metrics = metrics;
        ComputedAt = computedAt;
    }
}
