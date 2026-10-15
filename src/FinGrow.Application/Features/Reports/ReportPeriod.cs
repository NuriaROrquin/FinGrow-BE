namespace FinGrow.Application.Features.Reports;

internal static class ReportPeriod
{
    public const int MaxMonths = 36;

    public static DateOnly StartOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    public static int MonthCount(DateOnly from, DateOnly to) =>
        ((to.Year - from.Year) * 12) + to.Month - from.Month + 1;

    public static IReadOnlyList<DateOnly> Months(DateOnly from, DateOnly to)
    {
        var first = StartOfMonth(from);

        return Enumerable.Range(0, Math.Max(0, MonthCount(from, to)))
            .Select(offset => first.AddMonths(offset))
            .ToList();
    }
}
