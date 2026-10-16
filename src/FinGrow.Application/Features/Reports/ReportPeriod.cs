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

    public static (DateOnly From, DateOnly To) Previous(DateOnly from, DateOnly to)
    {
        var months = MonthCount(from, to);

        return (ShiftMonths(from, -months), ShiftMonths(to, -months));
    }

    private static DateOnly ShiftMonths(DateOnly date, int months)
    {
        var shifted = date.AddMonths(months);

        return IsEndOfMonth(date)
            ? new DateOnly(shifted.Year, shifted.Month, DateTime.DaysInMonth(shifted.Year, shifted.Month))
            : shifted;
    }

    private static bool IsEndOfMonth(DateOnly date) => date.Day == DateTime.DaysInMonth(date.Year, date.Month);
}
