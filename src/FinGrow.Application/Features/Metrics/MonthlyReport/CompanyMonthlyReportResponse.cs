namespace FinGrow.Application.Features.Metrics.MonthlyReport;

using FinGrow.Application.Features.Metrics.GetCompanyTrends;
using FinGrow.Application.Features.Metrics.Trends;
using FinGrow.Domain.Enums;

/// <summary>
/// Resumen agregado y anónimo de un mes cerrado de la empresa, comparado con el mes anterior
/// (HU-65, y el mismo contenido que manda por mail HU-82). Sin foto todavía, <c>Period</c> viaja en
/// <c>null</c>. Con menos empleados activos que <c>MinimumGroupSize</c>, <c>IsAnonymized</c> viaja en
/// <c>true</c>, los indicadores dicen por qué no se muestran y no hay participantes ni tendencias.
/// </summary>
public sealed record CompanyMonthlyReportResponse(
    string CompanyName,
    DateOnly? Period,
    DateOnly? PreviousPeriod,
    int MinimumGroupSize,
    bool IsAnonymized,
    int? ActiveEmployees,
    int? ParticipatingEmployees,
    FinancialHealthBand? AverageScoreBand,
    IReadOnlyList<CompanyReportIndicatorResponse> Indicators,
    CompanyTrendsResponse Trends)
{
    public static CompanyMonthlyReportResponse WithoutPeriod(string companyName, int minimumGroupSize) =>
        new(
            companyName,
            null,
            null,
            minimumGroupSize,
            false,
            null,
            null,
            null,
            Array.Empty<CompanyReportIndicatorResponse>(),
            CompanyTrendsResponse.WithoutPeriod(minimumGroupSize));
}

/// <summary>
/// Un indicador principal del mes y su valor del mes anterior. <c>Value</c> y <c>PreviousValue</c>
/// solo viajan con su estado en <c>Visible</c>, y <c>Change</c> solo cuando los dos son visibles.
/// </summary>
public sealed record CompanyReportIndicatorResponse(
    CompanyTrendIndicator Indicator,
    CompanyTrendUnit Unit,
    CompanyReportValueStatus Status,
    decimal? Value,
    CompanyReportValueStatus PreviousStatus,
    decimal? PreviousValue,
    decimal? Change);
