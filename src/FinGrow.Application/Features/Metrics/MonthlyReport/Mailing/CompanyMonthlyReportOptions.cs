namespace FinGrow.Application.Features.Metrics.MonthlyReport.Mailing;

using System.ComponentModel.DataAnnotations;

public sealed class CompanyMonthlyReportOptions
{
    public const string SectionName = "CompanyMonthlyReport";

    /// <summary>El panel de la empresa en el front, al que lleva el enlace del mail mensual (HU-82).</summary>
    [Required(ErrorMessage = "Falta configurar el panel de la empresa al que lleva el reporte mensual (CompanyMonthlyReport:PanelUrl).")]
    [Url]
    public string PanelUrl { get; init; } = string.Empty;
}
