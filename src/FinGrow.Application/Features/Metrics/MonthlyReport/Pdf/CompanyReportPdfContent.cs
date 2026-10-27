namespace FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;

/// <summary>
/// Todo lo que lleva el PDF del reporte mensual (HU-65 y HU-82), ya traducido y formateado. Lo arma
/// <see cref="CompanyReportPdfComposer"/> a partir de datos que ya pasaron por los umbrales de
/// anonimato; quien lo dibuja (<c>ICompanyReportPdfRenderer</c>) no decide qué se muestra ni cómo se
/// dice, solo dónde va cada cosa.
/// </summary>
public sealed record CompanyReportPdfContent(
    string Title,
    IReadOnlyList<PdfField> Header,
    string SummaryTitle,
    string? AnonymizedNotice,
    IReadOnlyList<PdfIndicatorCard> Indicators,
    string EvolutionTitle,
    IReadOnlyList<PdfLineChart> Charts,
    PdfSection Trends,
    PdfSection Concerns,
    PdfSection Recommendations,
    string PrivacyNote);

public sealed record PdfField(string Label, string Value);

/// <summary>Una de las tres tarjetas del resumen: el valor del mes o "Oculto", y las líneas de detalle.</summary>
public sealed record PdfIndicatorCard(string Label, string Value, IReadOnlyList<string> Details);

/// <summary>
/// Un gráfico de líneas con un punto por mes. Un valor <c>null</c> es un mes oculto por anonimato y
/// queda en blanco, igual que en el panel. Si <c>Notice</c> tiene texto, se muestra en lugar del gráfico.
/// </summary>
public sealed record PdfLineChart(
    string Title,
    string Description,
    string? Notice,
    IReadOnlyList<string> Months,
    IReadOnlyList<PdfChartSeries> Series,
    decimal? MinimumY,
    decimal? MaximumY);

public sealed record PdfChartSeries(
    string Name,
    string ColorHex,
    PdfChartAxis Axis,
    PdfChartUnit Unit,
    IReadOnlyList<decimal?> Values);

public enum PdfChartAxis
{
    Left = 1,
    Right = 2,
}

public enum PdfChartUnit
{
    Percentage = 1,
    Count = 2,
    Points = 3,
}

/// <summary>
/// Una sección con título y descripción. Si <c>Notice</c> tiene texto, va en lugar de los grupos
/// (sin datos, anonimizado o la IA no respondió).
/// </summary>
public sealed record PdfSection(
    string Title,
    string? Description,
    string? Notice,
    IReadOnlyList<PdfItemGroup> Groups,
    string? Footer);

public sealed record PdfItemGroup(string? Title, IReadOnlyList<PdfItem> Items);

/// <summary>
/// Un renglón de una sección: título, detalle y, a la derecha, un valor con su aclaración. El tono
/// elige el color del recuadro, como en el panel (verde lo que mejora, ámbar lo que empeora).
/// </summary>
public sealed record PdfItem(string Title, string? Detail, string? Value, string? ValueDetail, PdfTone Tone);

public enum PdfTone
{
    Neutral = 1,
    Positive = 2,
    Opportunity = 3,
    Warning = 4,
    Tip = 5,
}
