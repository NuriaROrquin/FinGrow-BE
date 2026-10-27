namespace FinGrow.Api.UnitTests.Reports;

using System.Text;
using FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;
using FinGrow.Infrastructure.Reports;

/// <summary>
/// Dibuja un PDF de verdad con MigraDoc y ScottPlot: comprueba que las fuentes y el logo viajan con la
/// API y que un mes oculto o una sección sin datos no rompen el documento.
/// </summary>
public class CompanyReportPdfRendererTests
{
    private static readonly string[] ParticipationDetails = { "8 de 10 empleados activos participaron" };
    private static readonly string[] CoursesDetails = { "+4 respecto de agosto de 2026" };

    [Fact]
    public void A_full_report_renders_as_a_pdf()
    {
        var pdf = new MigraDocCompanyReportPdfRenderer().Render(Content(withCharts: true));

        pdf.Length.ShouldBeGreaterThan(1000);
        Encoding.ASCII.GetString(pdf, 0, 5).ShouldBe("%PDF-");
    }

    [Fact]
    public void A_report_whose_sections_only_have_notices_also_renders()
    {
        var pdf = new MigraDocCompanyReportPdfRenderer().Render(Content(withCharts: false));

        Encoding.ASCII.GetString(pdf, 0, 5).ShouldBe("%PDF-");
    }

    private static CompanyReportPdfContent Content(bool withCharts)
    {
        var months = new[] { "Jul", "Ago", "Sept" };
        var notice = withCharts ? null : "No hay períodos para mostrar.";

        return new CompanyReportPdfContent(
            "Reporte de Bienestar Financiero",
            new[] { new PdfField("Empresa", "Acme S.A."), new PdfField("Período", "Septiembre de 2026") },
            "Resumen del mes",
            withCharts ? null : "En septiembre de 2026 hubo menos de 3 empleados activos.",
            new[]
            {
                new PdfIndicatorCard("Participación", "80%", ParticipationDetails),
                new PdfIndicatorCard("Salud financiera promedio", "Oculto", Array.Empty<string>()),
                new PdfIndicatorCard("Cursos completados", "6", CoursesDetails),
            },
            "Evolución",
            new[]
            {
                new PdfLineChart(
                    "Participación de Empleados",
                    "Usuarios activos vs empleados inscritos en el tiempo",
                    notice,
                    months,
                    new[]
                    {
                        new PdfChartSeries("Tasa de participación", "#8b5cf6", PdfChartAxis.Left, PdfChartUnit.Percentage, new decimal?[] { null, 50m, 80m }),
                        new PdfChartSeries("Empleados participantes", "#a78bfa", PdfChartAxis.Right, PdfChartUnit.Count, new decimal?[] { null, 5m, 8m }),
                    },
                    0m,
                    null),
                new PdfLineChart(
                    "Tendencia de Salud Financiera",
                    "Puntaje promedio de salud financiera de empleados",
                    notice,
                    months,
                    new[] { new PdfChartSeries("Puntaje de Salud", "#8b5cf6", PdfChartAxis.Left, PdfChartUnit.Points, new decimal?[] { 55m, null, 70m }) },
                    0m,
                    100m),
            },
            new PdfSection(
                "Tendencias",
                null,
                withCharts ? null : "Para ver tendencias hacen falta dos meses seguidos con datos.",
                withCharts
                    ? new[]
                    {
                        new PdfItemGroup("Tendencias positivas", new[] { new PdfItem("Participación", "De 50% a 80%", "+30 p.p.", null, PdfTone.Positive) }),
                        new PdfItemGroup("Oportunidades de mejora", new[] { new PdfItem("Ningún indicador empeoró de forma significativa.", null, null, null, PdfTone.Neutral) }),
                    }
                    : Array.Empty<PdfItemGroup>(),
                "Un cambio menor a 2 puntos se considera estable."),
            new PdfSection(
                "Principales Preocupaciones Financieras",
                "septiembre de 2026 · 5 empleados medidos",
                null,
                new[] { new PdfItemGroup(null, new[] { new PdfItem("1. Ahorro bajo", "Ahorraron menos del 10 % de sus ingresos del mes.", "60%", "3 de 5 empleados medidos", PdfTone.Neutral) }) },
                null),
            new PdfSection(
                "Recomendaciones",
                "Acciones para mejorar el bienestar de empleados",
                withCharts ? null : "No pudimos generar recomendaciones ahora.",
                withCharts
                    ? new[] { new PdfItemGroup(null, new[] { new PdfItem("Fondo de emergencia", "Ofrecé una charla sobre ahorro.", "Atención", null, PdfTone.Warning) }) }
                    : Array.Empty<PdfItemGroup>(),
                null),
            "Este reporte contiene solo métricas agregadas y anónimas de la plantilla.");
    }
}
