namespace FinGrow.Domain.ValueObjects;

using System.Globalization;
using FinGrow.Domain.Common;
using FinGrow.Domain.Errors;

public sealed class MetricsPeriod : ValueObject
{
    private MetricsPeriod(DateOnly start) => Start = start;

    public DateOnly Start { get; }

    public DateOnly End => Start.AddMonths(1).AddDays(-1);

    public DateTimeOffset StartInstant => ArgentinaTime.StartOfDay(Start);

    public DateTimeOffset EndInstantExclusive => ArgentinaTime.StartOfDay(End.AddDays(1));

    public static MetricsPeriod Of(int year, int month) => new(new DateOnly(year, month, 1));

    public static MetricsPeriod MonthOf(DateOnly date) => new(new DateOnly(date.Year, date.Month, 1));

    public static MetricsPeriod LastClosedMonth(DateOnly today) => MonthOf(today).Previous();

    public static MetricsPeriod FromStart(DateOnly start) =>
        start.Day == 1
            ? new MetricsPeriod(start)
            : throw new DomainException("Un periodo de metricas empieza el primer dia del mes.");

    public MetricsPeriod Previous() => new(Start.AddMonths(-1));

    public MetricsPeriod Next() => new(Start.AddMonths(1));

    public bool IsClosed(DateOnly today) => End < today;

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public IEnumerable<MetricsPeriod> Until(MetricsPeriod last)
    {
        ArgumentNullException.ThrowIfNull(last);

        for (var period = this; period.Start <= last.Start; period = period.Next())
        {
            yield return period;
        }
    }

    public override string ToString() => Start.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Start;
    }
}
