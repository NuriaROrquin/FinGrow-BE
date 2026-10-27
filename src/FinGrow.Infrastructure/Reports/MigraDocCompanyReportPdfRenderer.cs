namespace FinGrow.Infrastructure.Reports;

using FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;
using FinGrow.Application.Interfaces;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

/// <summary>
/// Dibuja el reporte mensual en A4 con MigraDoc (PDFsharp, licencia MIT), con el mismo orden que tenía
/// el PDF del panel: encabezado, resumen, evolución, tendencias, preocupaciones, recomendaciones y la
/// nota de privacidad, y "FinGrow" con el número de página al pie. No decide nada del contenido: lo
/// recibe ya armado en <see cref="CompanyReportPdfContent"/>.
/// </summary>
internal sealed class MigraDocCompanyReportPdfRenderer : ICompanyReportPdfRenderer
{
    private const double PageMarginMillimeters = 15;
    private const double ContentWidthMillimeters = 180;

    private static readonly Color TextColor = Rgb("#111827");
    private static readonly Color MutedColor = Rgb("#6b7280");
    private static readonly Color BorderColor = Rgb("#e5e7eb");
    private static readonly Color BrandColor = Rgb("#8b5cf6");

    private static string LogoPath => Path.Combine(AppContext.BaseDirectory, "Resources", "Images", "fingrow-logo.png");

    public byte[] Render(CompanyReportPdfContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ReportFonts.EnsureRegistered();

        var document = new Document();
        document.Info.Title = content.Title;
        DefineStyles(document);

        var section = document.AddSection();
        section.PageSetup = document.DefaultPageSetup.Clone();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromMillimeter(PageMarginMillimeters);
        section.PageSetup.BottomMargin = Unit.FromMillimeter(PageMarginMillimeters + 5);
        section.PageSetup.LeftMargin = Unit.FromMillimeter(PageMarginMillimeters);
        section.PageSetup.RightMargin = Unit.FromMillimeter(PageMarginMillimeters);

        AddFooter(section);
        AddHeader(section, content);

        AddSectionTitle(section, content.SummaryTitle);
        if (content.AnonymizedNotice is { } anonymized)
        {
            AddNotice(section, anonymized);
        }

        AddIndicators(section, content.Indicators);

        AddSectionTitle(section, content.EvolutionTitle);
        foreach (var chart in content.Charts)
        {
            AddChart(section, chart);
        }

        AddItemSection(section, content.Trends);
        AddItemSection(section, content.Concerns);
        AddItemSection(section, content.Recommendations);

        var privacy = section.AddParagraph(content.PrivacyNote);
        privacy.Style = "Small";
        privacy.Format.SpaceBefore = Unit.FromMillimeter(8);
        privacy.Format.Borders.Top.Width = 0.5;
        privacy.Format.Borders.Top.Color = BorderColor;
        privacy.Format.Borders.Distance = Unit.FromMillimeter(2);

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);

        return stream.ToArray();
    }

    private static void DefineStyles(Document document)
    {
        var normal = document.Styles[StyleNames.Normal]!;
        normal.Font.Name = ReportFonts.FamilyName;
        normal.Font.Size = 9.5;
        normal.Font.Color = TextColor;
        normal.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(1);

        var small = document.Styles.AddStyle("Small", StyleNames.Normal);
        small.Font.Size = 8;
        small.Font.Color = MutedColor;

        var sectionTitle = document.Styles.AddStyle("SectionTitle", StyleNames.Normal);
        sectionTitle.Font.Size = 13;
        sectionTitle.Font.Bold = true;
        sectionTitle.ParagraphFormat.SpaceBefore = Unit.FromMillimeter(7);
        sectionTitle.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(3);
        sectionTitle.ParagraphFormat.KeepWithNext = true;
        sectionTitle.ParagraphFormat.Borders.Bottom.Width = 0.5;
        sectionTitle.ParagraphFormat.Borders.Bottom.Color = BorderColor;
        sectionTitle.ParagraphFormat.Borders.Distance = Unit.FromMillimeter(1.5);
    }

    private static void AddFooter(Section section)
    {
        var footer = section.Footers.Primary.AddParagraph();
        footer.Style = "Small";
        footer.Format.AddTabStop(Unit.FromMillimeter(ContentWidthMillimeters), TabAlignment.Right);
        footer.AddText("FinGrow");
        footer.AddTab();
        footer.AddPageField();
        footer.AddText(" / ");
        footer.AddNumPagesField();
    }

    private static void AddHeader(Section section, CompanyReportPdfContent content)
    {
        var table = section.AddTable();
        table.AddColumn(Unit.FromMillimeter(95));
        table.AddColumn(Unit.FromMillimeter(ContentWidthMillimeters - 95));
        table.Borders.Bottom.Width = 0.5;
        table.Borders.Bottom.Color = BorderColor;

        var row = table.AddRow();
        row.BottomPadding = Unit.FromMillimeter(3);

        var brand = row.Cells[0].AddParagraph();
        var logo = brand.AddImage(LogoPath);
        logo.Height = Unit.FromMillimeter(11);
        logo.LockAspectRatio = true;
        brand.AddText("  ");
        var name = brand.AddFormattedText("FinGrow", TextFormat.Bold);
        name.Size = 18;
        name.Color = BrandColor;

        var title = row.Cells[0].AddParagraph(content.Title);
        title.Format.Font.Color = MutedColor;

        foreach (var field in content.Header)
        {
            var paragraph = row.Cells[1].AddParagraph();
            paragraph.Format.Alignment = ParagraphAlignment.Right;
            paragraph.Format.SpaceAfter = 0;
            var label = paragraph.AddFormattedText(field.Label + ": ");
            label.Color = MutedColor;
            paragraph.AddFormattedText(field.Value, TextFormat.Bold);
        }
    }

    private static void AddSectionTitle(Section section, string title) =>
        section.AddParagraph(title).Style = "SectionTitle";

    private static void AddNotice(Section section, string text)
    {
        var notice = section.AddParagraph(text);
        notice.Format.Font.Color = MutedColor;
        notice.Format.Borders.Width = 0.5;
        notice.Format.Borders.Color = BorderColor;
        notice.Format.Borders.Style = MigraDoc.DocumentObjectModel.BorderStyle.Dot;
        notice.Format.Borders.Distance = Unit.FromMillimeter(2.5);
        notice.Format.SpaceBefore = Unit.FromMillimeter(2);
        notice.Format.SpaceAfter = Unit.FromMillimeter(4);
    }

    private static void AddIndicators(Section section, IReadOnlyList<PdfIndicatorCard> indicators)
    {
        if (indicators.Count == 0)
        {
            return;
        }

        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.Borders.Color = BorderColor;
        table.LeftPadding = Unit.FromMillimeter(3);
        table.RightPadding = Unit.FromMillimeter(3);
        table.TopPadding = Unit.FromMillimeter(2.5);
        table.BottomPadding = Unit.FromMillimeter(2.5);

        var width = ContentWidthMillimeters / indicators.Count;
        for (var index = 0; index < indicators.Count; index++)
        {
            table.AddColumn(Unit.FromMillimeter(width));
        }

        var row = table.AddRow();
        for (var index = 0; index < indicators.Count; index++)
        {
            var card = indicators[index];
            var cell = row.Cells[index];

            cell.AddParagraph(card.Label).Format.Font.Color = MutedColor;

            var value = cell.AddParagraph(card.Value);
            value.Format.Font.Size = 18;
            value.Format.Font.Bold = true;
            value.Format.SpaceAfter = Unit.FromMillimeter(1.5);

            foreach (var detail in card.Details)
            {
                cell.AddParagraph(detail).Style = "Small";
            }
        }
    }

    private static void AddChart(Section section, PdfLineChart chart)
    {
        var title = section.AddParagraph();
        title.Format.KeepWithNext = true;
        title.Format.SpaceBefore = Unit.FromMillimeter(3);
        title.AddFormattedText(chart.Title, TextFormat.Bold);

        var description = section.AddParagraph(chart.Description);
        description.Style = "Small";
        description.Format.KeepWithNext = true;

        if (chart.Notice is { } notice)
        {
            AddNotice(section, notice);
            return;
        }

        AddLegend(section, chart.Series);

        var png = ReportChartRenderer.Render(chart);
        var image = section.AddParagraph().AddImage("base64:" + Convert.ToBase64String(png));
        image.Width = Unit.FromMillimeter(ContentWidthMillimeters);
        image.LockAspectRatio = true;
    }

    /// <summary>
    /// La leyenda va en una fila arriba del gráfico, con un punto del color de cada línea: el panel
    /// no la necesita porque tiene tooltip, pero en el PDF es la única forma de distinguirlas.
    /// </summary>
    private static void AddLegend(Section section, IReadOnlyList<PdfChartSeries> series)
    {
        var legend = section.AddParagraph();
        legend.Style = "Small";
        legend.Format.KeepWithNext = true;
        legend.Format.SpaceBefore = Unit.FromMillimeter(1.5);

        for (var index = 0; index < series.Count; index++)
        {
            if (index > 0)
            {
                legend.AddText("      ");
            }

            legend.AddFormattedText("\u25CF ").Color = Rgb(series[index].ColorHex);
            legend.AddText(series[index].Name);
        }
    }

    private static void AddItemSection(Section section, PdfSection content)
    {
        AddSectionTitle(section, content.Title);

        if (content.Description is { } description)
        {
            var paragraph = section.AddParagraph(description);
            paragraph.Style = "Small";
            paragraph.Format.KeepWithNext = true;
        }

        if (content.Notice is { } notice)
        {
            AddNotice(section, notice);
        }

        foreach (var group in content.Groups)
        {
            if (group.Title is { } groupTitle)
            {
                var paragraph = section.AddParagraph();
                paragraph.Format.SpaceBefore = Unit.FromMillimeter(2);
                paragraph.Format.KeepWithNext = true;
                paragraph.AddFormattedText(groupTitle, TextFormat.Bold);
            }

            AddItems(section, group.Items);
        }

        if (content.Footer is { } footer)
        {
            var paragraph = section.AddParagraph(footer);
            paragraph.Style = "Small";
            paragraph.Format.SpaceBefore = Unit.FromMillimeter(2);
        }
    }

    private static void AddItems(Section section, IReadOnlyList<PdfItem> items)
    {
        var table = section.AddTable();
        table.AddColumn(Unit.FromMillimeter(ContentWidthMillimeters - 45));
        table.AddColumn(Unit.FromMillimeter(45));
        table.LeftPadding = Unit.FromMillimeter(3);
        table.RightPadding = Unit.FromMillimeter(3);
        table.TopPadding = Unit.FromMillimeter(2);
        table.BottomPadding = Unit.FromMillimeter(2);

        foreach (var item in items)
        {
            var row = table.AddRow();
            row.Borders.Bottom.Width = 2;
            row.Borders.Bottom.Color = Colors.White;

            if (Shading(item.Tone) is { } shading)
            {
                row.Shading.Color = shading;
            }

            var title = row.Cells[0].AddParagraph();
            title.AddFormattedText(item.Title, TextFormat.Bold);

            if (item.Detail is { } detail)
            {
                row.Cells[0].AddParagraph(detail).Format.Font.Color = MutedColor;
            }

            if (item.Value is { } value)
            {
                var paragraph = row.Cells[1].AddParagraph();
                paragraph.Format.Alignment = ParagraphAlignment.Right;
                paragraph.AddFormattedText(value, TextFormat.Bold);
            }

            if (item.ValueDetail is { } valueDetail)
            {
                var paragraph = row.Cells[1].AddParagraph(valueDetail);
                paragraph.Style = "Small";
                paragraph.Format.Alignment = ParagraphAlignment.Right;
            }
        }
    }

    /// <summary>Los mismos colores suaves que las tarjetas del panel: verde, ámbar y azul.</summary>
    private static Color? Shading(PdfTone tone) => tone switch
    {
        PdfTone.Positive => Rgb("#ecfdf5"),
        PdfTone.Opportunity or PdfTone.Warning => Rgb("#fffbeb"),
        PdfTone.Tip => Rgb("#eff6ff"),
        _ => null,
    };

    private static Color Rgb(string hex) =>
        new(
            Convert.ToByte(hex.Substring(1, 2), 16),
            Convert.ToByte(hex.Substring(3, 2), 16),
            Convert.ToByte(hex.Substring(5, 2), 16));
}
