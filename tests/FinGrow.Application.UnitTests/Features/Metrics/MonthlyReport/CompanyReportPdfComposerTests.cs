namespace FinGrow.Application.UnitTests.Features.Metrics.MonthlyReport;

using FinGrow.Application.Features.Metrics.GetCompanyMetrics;
using FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using static MonthlyReportMailingScenario;

public class CompanyReportPdfComposerTests
{
    private static readonly MetricsPeriod July = MetricsPeriod.Of(2026, 7);
    private static readonly string[] JulyToSeptember = { "Jul", "Ago", "Sept" };
    private static readonly string[] AugustAndSeptemberInPortuguese = { "Ago.", "Set." };
    private static readonly string[] AugustAndSeptemberInEnglish = { "Aug", "Sep" };

    private readonly MonthlyReportMailingScenario _scenario = new();

    [Fact]
    public async Task The_header_and_the_summary_say_the_same_as_the_panel_pdf_did()
    {
        _scenario.PhotographAugustAndSeptember();

        var content = await ComposeAsync(Language.es);

        content.Title.ShouldBe("Reporte de Bienestar Financiero");
        content.Header.Select(field => (field.Label, field.Value)).ShouldBe(new[]
        {
            ("Empresa", "Acme S.A."),
            ("Período", "Septiembre de 2026"),
            ("Comparado con", "Agosto de 2026"),
            ("Generado el", "01/10/2026 05:00"),
        });
        content.AnonymizedNotice.ShouldBeNull();
        content.Indicators.Select(card => (card.Label, card.Value, string.Join(" | ", card.Details))).ShouldBe(new[]
        {
            ("Participación", "80%", "8 de 10 empleados activos participaron | +30 p.p. respecto de agosto de 2026"),
            ("Salud financiera promedio", "70/100", "Nivel: Estable | +10 puntos respecto de agosto de 2026"),
            ("Cursos completados", "6", "+4 respecto de agosto de 2026"),
        });
        content.PrivacyNote.ShouldEndWith("Los indicadores de grupos de menos de 3 empleados no se muestran.");
    }

    [Fact]
    public async Task The_charts_have_one_point_per_photographed_month_and_leave_hidden_months_blank()
    {
        _scenario.TakeSnapshot(July, PeriodMetrics.From(2, 2, 20, 0, 0, 0, 0, 2, 140));
        _scenario.PhotographAugustAndSeptember();

        var content = await ComposeAsync(Language.es);

        var participation = content.Charts[0];
        participation.Title.ShouldBe("Participación de Empleados");
        participation.Notice.ShouldBeNull();
        participation.Months.ShouldBe(JulyToSeptember);
        participation.Series[0].Values.ShouldBe(new decimal?[] { null, 50m, 80m });
        participation.Series[0].Axis.ShouldBe(PdfChartAxis.Left);
        participation.Series[1].Values.ShouldBe(new decimal?[] { null, 5m, 8m });
        participation.Series[1].Axis.ShouldBe(PdfChartAxis.Right);
        var health = content.Charts[1];
        health.Series.ShouldHaveSingleItem().Values.ShouldBe(new decimal?[] { null, 60m, 70m });
        health.MinimumY.ShouldBe(0m);
        health.MaximumY.ShouldBe(100m);
    }

    [Fact]
    public async Task A_score_hidden_in_every_month_replaces_the_chart_with_the_reason()
    {
        _scenario.TakeSnapshot(August, PeriodMetrics.From(10, 6, 100, 0, 0, 0, 0, 2, 120));
        _scenario.TakeSnapshot(September, PeriodMetrics.From(10, 7, 100, 0, 0, 0, 0, 2, 140));

        var content = await ComposeAsync(Language.es);

        content.Charts[1].Notice.ShouldBe(
            "El puntaje se oculta en los meses con pocos empleados con actividad, para no identificar a nadie.");
        content.Charts[0].Notice.ShouldBeNull();
    }

    [Fact]
    public async Task Trends_are_split_into_positive_and_opportunities_with_their_changes()
    {
        _scenario.PhotographAugustAndSeptember();

        var content = await ComposeAsync(Language.es);

        content.Trends.Notice.ShouldBeNull();
        content.Trends.Footer.ShouldBe(
            "Se calculan a partir de indicadores agregados y anónimos. Un cambio menor a 2 puntos se considera estable.");
        var positive = content.Trends.Groups[0];
        positive.Title.ShouldBe("Tendencias positivas");
        var participation = positive.Items.First(item => item.Title == "Participación");
        participation.Detail.ShouldBe("De 50% a 80%");
        participation.Value.ShouldBe("+30 p.p.");
        participation.Tone.ShouldBe(PdfTone.Positive);
        var opportunities = content.Trends.Groups[1];
        opportunities.Title.ShouldBe("Oportunidades de mejora");
        opportunities.Items.ShouldAllBe(item => item.Tone == PdfTone.Opportunity);
    }

    [Fact]
    public async Task Without_the_previous_month_the_trends_say_so()
    {
        _scenario.TakeSnapshot(September, PeriodMetrics.From(5, 4, 50, 0, 0, 0, 0, 0, 0));

        var content = await ComposeAsync(Language.es);

        content.Trends.Groups.ShouldBeEmpty();
        content.Trends.Notice.ShouldBe(
            "Para ver tendencias hacen falta dos meses seguidos con datos. Por ahora solo está septiembre de 2026.");
        content.Indicators[0].Details.ShouldContain("Sin datos de agosto de 2026 para comparar.");
    }

    [Fact]
    public async Task Concerns_and_recommendations_come_from_the_same_month_as_the_report()
    {
        _scenario.PhotographAugustAndSeptember();

        var content = await ComposeAsync(Language.es);

        content.Concerns.Description.ShouldBe("septiembre de 2026 · 5 empleados medidos");
        var concern = content.Concerns.Groups.ShouldHaveSingleItem().Items.ShouldHaveSingleItem();
        concern.Title.ShouldBe("1. Ahorro bajo");
        concern.Value.ShouldBe("60%");
        concern.ValueDetail.ShouldBe("3 de 5 empleados medidos");
        content.Concerns.Footer.ShouldBe(
            "Solo se muestran las preocupaciones que afectan al menos a 3 empleados, para cuidar el anonimato.");
        var recommendations = content.Recommendations.Groups.ShouldHaveSingleItem().Items;
        recommendations.Select(item => (item.Value, item.Tone)).ShouldBe(new (string?, PdfTone)[]
        {
            ("Vas bien", PdfTone.Positive),
            ("Atención", PdfTone.Warning),
            ("Sugerencia", PdfTone.Tip),
        });
        content.Recommendations.Footer.ShouldNotBeNull().ShouldStartWith(
            "Se generan automáticamente a partir de indicadores agregados y anónimos de septiembre de 2026.");
        _scenario.Ai.CompanyRecommendationRequests.ShouldHaveSingleItem().Month.ShouldBe(9);
    }

    [Fact]
    public async Task Few_measured_employees_hide_concerns_and_recommendations_and_say_why()
    {
        _scenario.TakeSnapshot(August, PeriodMetrics.From(10, 6, 100, 0, 0, 0, 0, 2, 120));
        _scenario.TakeSnapshot(September, PeriodMetrics.From(10, 7, 100, 0, 0, 0, 0, 2, 140));

        var content = await ComposeAsync(Language.es);

        const string Expected =
            "En septiembre de 2026 menos de 3 empleados tuvieron actividad suficiente para medir su salud financiera. Se muestra cuando hay suficientes para no identificar a nadie.";
        content.Concerns.Notice.ShouldBe(Expected);
        content.Concerns.Footer.ShouldBeNull();
        content.Recommendations.Notice.ShouldBe(Expected);
        content.Recommendations.Groups.ShouldBeEmpty();
        _scenario.Ai.CompanyRecommendationRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task English_and_portuguese_use_their_own_texts_and_decimal_separators()
    {
        _scenario.TakeSnapshot(August, PeriodMetrics.From(3, 1, 30, 0, 0, 0, 0, 0, 0));
        _scenario.TakeSnapshot(September, PeriodMetrics.From(3, 2, 30, 0, 0, 0, 0, 0, 0));

        var english = await ComposeAsync(Language.en);
        var portuguese = await ComposeAsync(Language.pt);
        var spanish = await ComposeAsync(Language.es);

        english.Title.ShouldBe("Financial Wellbeing Report");
        english.Indicators[0].Label.ShouldBe("Participation");
        english.Indicators[0].Value.ShouldBe("66.7%");
        english.Header[1].Value.ShouldBe("September 2026");
        portuguese.Title.ShouldBe("Relatório de Bem-Estar Financeiro");
        portuguese.Indicators[0].Value.ShouldBe("66,7%");
        portuguese.Header[1].Value.ShouldBe("Setembro de 2026");
        portuguese.Charts[0].Months.ShouldBe(AugustAndSeptemberInPortuguese);
        english.Charts[0].Months.ShouldBe(AugustAndSeptemberInEnglish);
        spanish.Indicators[0].Value.ShouldBe("66,7%");
    }

    [Theory]
    [InlineData(Language.es, "reporte-empresa_202609.pdf")]
    [InlineData(Language.en, "company-report_202609.pdf")]
    [InlineData(Language.pt, "relatorio-empresa_202609.pdf")]
    public void The_file_name_has_the_month_of_the_report(Language language, string expected) =>
        CompanyReportPdfComposer.FileName(language, September.Start).ShouldBe(expected);

    private async Task<CompanyReportPdfContent> ComposeAsync(Language language)
    {
        var report = await _scenario.SeptemberReportAsync();
        var metrics = CompanyMetricsSeries.Build(
            _scenario.Snapshots.CompanySnapshots.Where(snapshot => snapshot.CompanyId == _scenario.Company.Id),
            _scenario.Company.FiscalYearStartMonth,
            Policy);
        var insights = await _scenario.InsightsBuilder.BuildAsync(_scenario.Company, September.Start, CancellationToken.None);

        return CompanyReportPdfComposer.Compose(report, metrics, insights, language, Now);
    }
}
