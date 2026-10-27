namespace FinGrow.Infrastructure.Reports;

using System.Globalization;
using FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;
using ScottPlot;

/// <summary>
/// Dibuja un gráfico de evolución del reporte como PNG, con ScottPlot (licencia MIT). Imita los de
/// recharts del panel: una línea por serie, un punto por mes, ejes de 0 a un tope redondo con cinco
/// marcas, el primer y el último mes contra los bordes y, en un mes oculto por anonimato, un hueco en
/// la línea en lugar de un cero. La leyenda la escribe el PDF arriba del gráfico.
/// </summary>
internal static class ReportChartRenderer
{
    /// <summary>Tamaño lógico: con 720 px a lo ancho de la hoja, la letra de 12 px queda como en el panel.</summary>
    private const int WidthPixels = 720;
    private const int HeightPixels = 260;

    /// <summary>Se dibuja al triple de resolución para que no se vea pixelado al imprimir.</summary>
    private const int Scale = 3;

    private static readonly object Gate = new();
    private static bool _fontsRegistered;

    public static byte[] Render(PdfLineChart chart)
    {
        ArgumentNullException.ThrowIfNull(chart);
        RegisterFonts();

        var plot = new Plot();
        plot.ScaleFactor = Scale;
        plot.Font.Set(ReportFonts.FamilyName);

        var positions = Enumerable.Range(0, chart.Months.Count).Select(index => (double)index).ToArray();
        plot.Axes.Bottom.SetTicks(positions, chart.Months.ToArray());
        SetLimitsX(plot, chart.Months.Count);

        foreach (var series in chart.Series)
        {
            AddSeries(plot, series);
        }

        var left = chart.Series.Where(series => series.Axis == PdfChartAxis.Left).ToList();
        var leftTicks = ChartScale.Ticks(chart.MinimumY ?? 0m, chart.MaximumY, Highest(left));
        plot.Axes.Left.TickGenerator = TickGenerator(leftTicks, left.FirstOrDefault()?.Unit);
        plot.Axes.SetLimitsY((double)leftTicks[0], (double)leftTicks[^1]);

        var right = chart.Series.Where(series => series.Axis == PdfChartAxis.Right).ToList();

        if (right.Count > 0)
        {
            var rightTicks = ChartScale.Ticks(0m, null, Highest(right));
            plot.Axes.Right.TickGenerator = TickGenerator(rightTicks, right[0].Unit);
            plot.Axes.SetLimitsY((double)rightTicks[0], (double)rightTicks[^1], plot.Axes.Right);
        }

        plot.Grid.MajorLineColor = Colors.Black.WithAlpha(0.12);
        plot.Grid.MajorLinePattern = LinePattern.Dashed;

        return plot.GetImageBytes(WidthPixels * Scale, HeightPixels * Scale, ImageFormat.Png);
    }

    /// <summary>
    /// Como en el panel, el primer y el último mes van contra los bordes; queda un margen mínimo para
    /// que el punto no se corte. Con un solo mes, el punto queda al medio.
    /// </summary>
    private static void SetLimitsX(Plot plot, int months)
    {
        if (months <= 1)
        {
            plot.Axes.SetLimitsX(-0.5, 0.5);
            return;
        }

        var margin = (months - 1) * 0.02;
        plot.Axes.SetLimitsX(-margin, months - 1 + margin);
    }

    /// <summary>Una serie se parte en tramos sin huecos, así un mes oculto no se une con una línea.</summary>
    private static void AddSeries(Plot plot, PdfChartSeries series)
    {
        var color = Color.FromHex(series.ColorHex);

        foreach (var run in Runs(series.Values))
        {
            var scatter = plot.Add.Scatter(
                run.Select(point => (double)point.Index).ToArray(),
                run.Select(point => (double)point.Value).ToArray());

            scatter.Color = color;
            scatter.LineWidth = 2;
            scatter.MarkerSize = 6;

            if (series.Axis == PdfChartAxis.Right)
            {
                scatter.Axes.YAxis = plot.Axes.Right;
            }
        }
    }

    private static List<List<(int Index, decimal Value)>> Runs(IReadOnlyList<decimal?> values)
    {
        var runs = new List<List<(int Index, decimal Value)>>();
        List<(int Index, decimal Value)>? current = null;

        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] is not { } value)
            {
                current = null;
                continue;
            }

            if (current is null)
            {
                current = new List<(int Index, decimal Value)>();
                runs.Add(current);
            }

            current.Add((index, value));
        }

        return runs;
    }

    private static ScottPlot.TickGenerators.NumericManual TickGenerator(IReadOnlyList<decimal> ticks, PdfChartUnit? unit)
    {
        var generator = new ScottPlot.TickGenerators.NumericManual();

        foreach (var tick in ticks)
        {
            var label = tick.ToString("0.##", CultureInfo.InvariantCulture);
            generator.AddMajor((double)tick, unit == PdfChartUnit.Percentage ? label + "%" : label);
        }

        return generator;
    }

    private static decimal Highest(IEnumerable<PdfChartSeries> series) =>
        series.SelectMany(item => item.Values).Max() ?? 0m;

    private static void RegisterFonts()
    {
        lock (Gate)
        {
            if (_fontsRegistered)
            {
                return;
            }

            Fonts.AddFontFile(ReportFonts.FamilyName, ReportFonts.RegularPath, bold: false, italic: false);
            Fonts.AddFontFile(ReportFonts.FamilyName, ReportFonts.BoldPath, bold: true, italic: false);
            _fontsRegistered = true;
        }
    }
}
