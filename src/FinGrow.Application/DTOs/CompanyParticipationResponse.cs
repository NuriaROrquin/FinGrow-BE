namespace FinGrow.Application.DTOs;

using FinGrow.Application.Features.Metrics;
using FinGrow.Domain.ValueObjects;

public sealed record CompanyParticipationResponse(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int TotalEmployees,
    int ActiveEmployees,
    int? ParticipatingEmployees,
    decimal? ParticipationRate,
    bool IsParticipationHidden,
    int MinimumGroupSize)
{
    public static CompanyParticipationResponse From(MetricsPeriod period, int totalEmployees, PeriodMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(metrics);

        var hidden = metrics.ActiveEmployees > 0 && !AggregatePrivacy.CanDisclose(metrics.ActiveEmployees);

        return new CompanyParticipationResponse(
            period.Start,
            period.End,
            totalEmployees,
            metrics.ActiveEmployees,
            hidden ? null : metrics.ParticipatingEmployees,
            hidden ? null : metrics.ParticipationRate,
            hidden,
            AggregatePrivacy.MinimumGroupSize);
    }
}
