namespace FinGrow.Application.Interfaces;

using FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;

/// <summary>Dibuja el PDF del reporte mensual de la empresa (HU-65 y HU-82) en A4.</summary>
public interface ICompanyReportPdfRenderer
{
    byte[] Render(CompanyReportPdfContent content);
}
