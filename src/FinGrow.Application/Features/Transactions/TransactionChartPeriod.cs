namespace FinGrow.Application.Features.Transactions;

using Interfaces;

internal static class TransactionChartPeriod
{
    public static (DateOnly From, DateOnly To) LastSixCalendarMonths(IDateTimeProvider dateTimeProvider)
    {
        var currentMonth = new DateOnly(dateTimeProvider.Today.Year, dateTimeProvider.Today.Month, 1);
        var from = currentMonth.AddMonths(-5);
        var to = currentMonth.AddMonths(1).AddDays(-1);

        return (from, to);
    }
}
