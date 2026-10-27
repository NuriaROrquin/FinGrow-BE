namespace FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;

using FinGrow.Application.Features.Metrics.GetCompanyInsights;
using FinGrow.Application.Features.Metrics.GetCompanyMetrics;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// El único PDF del reporte mensual: lo descarga el panel (HU-65) y lo adjunta el mail mensual
/// (HU-82). Junta el reporte del mes, los meses con foto hasta ese mes para los gráficos y las
/// preocupaciones y recomendaciones de ese mismo mes, y lo dibuja en el idioma de la empresa.
/// </summary>
internal sealed class CompanyReportPdfBuilder(
    CompanyMonthlyReportBuilder reports,
    CompanyInsightsBuilder insights,
    IMetricsSnapshotRepository snapshots,
    MetricsAnonymizationPolicy anonymizationPolicy,
    ICompanyReportPdfRenderer renderer)
{
    /// <summary>
    /// Arma el PDF del mes pedido. Devuelve el reporte con el que se armó, para que el mail use los
    /// mismos meses, o <c>null</c> si ese mes todavía no tiene foto.
    /// </summary>
    public async Task<CompanyReportPdf?> BuildAsync(
        Company company,
        MetricsPeriod period,
        DateTimeOffset generatedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(period);

        if (await reports.BuildAsync(company, period, cancellationToken) is not { } report)
        {
            return null;
        }

        var metrics = CompanyMetricsSeries.Build(
            await snapshots.ListCompanyAsync(company.Id, null, period.Start, cancellationToken),
            company.FiscalYearStartMonth,
            anonymizationPolicy);

        var content = CompanyReportPdfComposer.Compose(
            report,
            metrics,
            await insights.BuildAsync(company, period.Start, cancellationToken),
            company.Language,
            generatedAt);

        return new CompanyReportPdf(
            report,
            CompanyReportPdfComposer.FileName(company.Language, period.Start),
            renderer.Render(content));
    }
}

/// <summary>El PDF ya dibujado, con el reporte del que salió.</summary>
internal sealed record CompanyReportPdf(CompanyMonthlyReportResponse Report, string FileName, byte[] Content);
