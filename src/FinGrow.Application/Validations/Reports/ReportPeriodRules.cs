namespace FinGrow.Application.Validations.Reports;

using FinGrow.Application.Features.Reports;

internal static class ReportPeriodRules
{
    public static readonly string Message =
        $"El periodo tiene que terminar despues de empezar y abarcar como mucho {ReportPeriod.MaxMonths} meses.";

    public static bool IsValid(DateOnly from, DateOnly to) =>
        from <= to && ReportPeriod.MonthCount(from, to) <= ReportPeriod.MaxMonths;
}
