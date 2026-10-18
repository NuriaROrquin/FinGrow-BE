namespace FinGrow.Application.Features.Transactions;

using Interfaces;

internal static class TransactionChartPeriod
{
    public static (DateOnly From, DateOnly To) LastSixCalendarMonths(IDateTimeProvider dateTimeProvider) =>
        SixCalendarMonthsEnding(dateTimeProvider.Today);

    public static (DateOnly From, DateOnly To) SixCalendarMonthsEnding(DateOnly anyDayOfLastMonth)
    {
        var lastMonth = new DateOnly(anyDayOfLastMonth.Year, anyDayOfLastMonth.Month, 1);
        var from = lastMonth.AddMonths(-5);
        var to = lastMonth.AddMonths(1).AddDays(-1);

        return (from, to);
    }
}
