namespace FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;

using System.Globalization;
using FinGrow.Application.Features.Metrics.GetCompanyInsights;
using FinGrow.Application.Features.Metrics.GetCompanyMetrics;
using FinGrow.Application.Features.Metrics.GetCompanyTrends;
using FinGrow.Application.Features.Metrics.Trends;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;

/// <summary>
/// Arma el contenido del PDF del reporte con las mismas reglas que seguían las tarjetas del panel
/// cuando el PDF lo armaba el front (<c>company-report-pdf-export.tsx</c>, <c>TrendsContent</c>,
/// <c>FinancialConcernsCard</c> y <c>CompanyRecommendationsCard</c>). No calcula ni filtra nada: todo
/// lo que recibe ya pasó por los umbrales de anonimato.
/// </summary>
internal static class CompanyReportPdfComposer
{
    private const string PrimaryColor = "#8b5cf6";
    private const string SecondaryColor = "#a78bfa";

    public static string FileName(Language language, DateOnly period) =>
        CompanyReportTexts.For(language).FileName
        + "_"
        + period.ToString("yyyyMM", CultureInfo.InvariantCulture)
        + ".pdf";

    public static CompanyReportPdfContent Compose(
        CompanyMonthlyReportResponse report,
        IReadOnlyList<CompanyMetricsResponse> metrics,
        CompanyInsightsResponse insights,
        Language language,
        DateTimeOffset generatedAt)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(insights);

        if (report.Period is not { } period)
        {
            throw new ArgumentException("El PDF solo se arma para un mes con foto.", nameof(report));
        }

        var texts = CompanyReportTexts.For(language);
        var locale = texts.Locale;
        var minimum = locale.Number(report.MinimumGroupSize);
        var month = locale.MonthAndYear(period);
        var previousMonth = report.PreviousPeriod is { } previous ? locale.MonthAndYear(previous) : string.Empty;

        var header = new List<PdfField>
        {
            new(texts.Company, report.CompanyName),
            new(texts.Period, Capitalize(month)),
        };

        if (report.PreviousPeriod is not null)
        {
            header.Add(new PdfField(texts.ComparedWith, Capitalize(previousMonth)));
        }

        header.Add(new PdfField(texts.GeneratedAt, ReportLocale.DateAndTime(generatedAt)));

        return new CompanyReportPdfContent(
            texts.Title,
            header,
            texts.SummaryTitle,
            report.IsAnonymized ? texts.Anonymized(month, minimum) : null,
            report.Indicators.Select(indicator => IndicatorCard(indicator, report, texts, previousMonth, minimum)).ToList(),
            texts.EvolutionTitle,
            Charts(metrics, texts),
            Trends(report.Trends, texts, minimum),
            Concerns(insights, texts),
            Recommendations(insights, texts),
            texts.PrivacyNote(minimum));
    }

    private static PdfIndicatorCard IndicatorCard(
        CompanyReportIndicatorResponse indicator,
        CompanyMonthlyReportResponse report,
        CompanyReportTexts texts,
        string previousMonth,
        string minimum)
    {
        var details = new List<string>();
        var label = texts.Indicators(indicator.Indicator);

        if (indicator.Status != CompanyReportValueStatus.Visible || indicator.Value is not { } value)
        {
            if (HiddenReason(indicator.Status, texts, minimum) is { } reason)
            {
                details.Add(reason);
            }

            return new PdfIndicatorCard(label, texts.Hidden, details);
        }

        if (indicator.Indicator == CompanyTrendIndicator.ParticipationRate
            && report.ParticipatingEmployees is { } participating
            && report.ActiveEmployees is { } active)
        {
            details.Add(texts.Participants(texts.Locale.Number(participating), texts.Locale.Number(active)));
        }
        else if (indicator.Indicator == CompanyTrendIndicator.AverageFinancialHealthScore
                 && report.AverageScoreBand is { } band)
        {
            details.Add(texts.Band(texts.Bands(band)));
        }

        if (Comparison(indicator, texts, previousMonth, minimum) is { } comparison)
        {
            details.Add(comparison);
        }

        return new PdfIndicatorCard(label, texts.Value(indicator.Unit, value), details);
    }

    private static string? HiddenReason(CompanyReportValueStatus status, CompanyReportTexts texts, string minimum) =>
        status switch
        {
            CompanyReportValueStatus.TooFewActiveEmployees => texts.HiddenTooFewActive(minimum),
            CompanyReportValueStatus.TooFewScoredEmployees => texts.HiddenTooFewScored(minimum),
            _ => null,
        };

    private static string? Comparison(
        CompanyReportIndicatorResponse indicator,
        CompanyReportTexts texts,
        string previousMonth,
        string minimum) =>
        indicator.PreviousStatus switch
        {
            CompanyReportValueStatus.NoData => texts.PreviousNoData(previousMonth),
            CompanyReportValueStatus.TooFewActiveEmployees => texts.PreviousTooFewActive(previousMonth, minimum),
            CompanyReportValueStatus.TooFewScoredEmployees => texts.PreviousTooFewScored(previousMonth, minimum),
            _ => indicator.Change switch
            {
                null => null,
                0m => texts.NoChange(previousMonth),
                { } change => texts.Change(texts.ChangeOf(indicator.Unit, change), previousMonth),
            },
        };

    private static List<PdfLineChart> Charts(IReadOnlyList<CompanyMetricsResponse> metrics, CompanyReportTexts texts)
    {
        var crossesYears = metrics.Select(metric => metric.Period.Year).Distinct().Count() > 1;
        var months = metrics.Select(metric => texts.Locale.ShortMonth(metric.Period, crossesYears)).ToList();
        var scores = metrics
            .Select(metric => metric.IsMetricsHidden ? null : metric.AverageFinancialHealthScore)
            .ToList();

        var participation = new PdfLineChart(
            texts.ParticipationChartTitle,
            texts.ParticipationChartDescription,
            metrics.Count == 0 ? texts.NoPeriods : null,
            months,
            new[]
            {
                new PdfChartSeries(
                    texts.ParticipationRateSeries,
                    PrimaryColor,
                    PdfChartAxis.Left,
                    PdfChartUnit.Percentage,
                    metrics.Select(metric => metric.IsMetricsHidden ? null : metric.ParticipationRate).ToList()),
                new PdfChartSeries(
                    texts.ParticipatingEmployeesSeries,
                    SecondaryColor,
                    PdfChartAxis.Right,
                    PdfChartUnit.Count,
                    metrics
                        .Select(metric => metric.IsMetricsHidden ? null : (decimal?)metric.ParticipatingEmployees)
                        .ToList()),
            },
            0m,
            null);

        var health = new PdfLineChart(
            texts.HealthChartTitle,
            texts.HealthChartDescription,
            metrics.Count == 0
                ? texts.NoPeriods
                : scores.TrueForAll(score => score is null) ? texts.ScoreHiddenAll : null,
            months,
            new[]
            {
                new PdfChartSeries(texts.HealthScoreSeries, PrimaryColor, PdfChartAxis.Left, PdfChartUnit.Points, scores),
            },
            0m,
            100m);

        return new List<PdfLineChart> { participation, health };
    }

    private static PdfSection Trends(CompanyTrendsResponse trends, CompanyReportTexts texts, string minimum)
    {
        var locale = texts.Locale;
        string? notice = null;

        if (trends.Period is not { } period)
        {
            notice = texts.TrendsNoPeriod;
        }
        else if (trends.PreviousPeriod is not { } previousPeriod)
        {
            notice = texts.TrendsNoPreviousPeriod(locale.MonthAndYear(period));
        }
        else if (trends.IsAnonymized)
        {
            notice = texts.TrendsAnonymized(minimum);
        }
        else if (trends.PositiveTrends.Count == 0 && trends.Opportunities.Count == 0)
        {
            notice = texts.NoTrendChanges(locale.MonthAndYear(period), locale.MonthAndYear(previousPeriod));
        }

        var groups = notice is not null
            ? Array.Empty<PdfItemGroup>()
            : new[]
            {
                TrendGroup(texts.PositiveTrendsTitle, texts.NoPositiveTrends, trends.PositiveTrends, PdfTone.Positive, texts),
                TrendGroup(texts.OpportunitiesTitle, texts.NoOpportunities, trends.Opportunities, PdfTone.Opportunity, texts),
            };

        return new PdfSection(texts.TrendsTitle, null, notice, groups, texts.TrendsFooter);
    }

    private static PdfItemGroup TrendGroup(
        string title,
        string empty,
        IReadOnlyList<CompanyTrend> trends,
        PdfTone tone,
        CompanyReportTexts texts) =>
        new(
            title,
            trends.Count == 0
                ? new[] { new PdfItem(empty, null, null, null, PdfTone.Neutral) }
                : trends
                    .Select(trend => new PdfItem(
                        texts.Indicators(trend.Indicator),
                        texts.FromTo(texts.Value(trend.Unit, trend.PreviousValue), texts.Value(trend.Unit, trend.CurrentValue)),
                        texts.ChangeOf(trend.Unit, trend.Change),
                        null,
                        tone))
                    .ToArray());

    private static PdfSection Concerns(CompanyInsightsResponse insights, CompanyReportTexts texts)
    {
        var locale = texts.Locale;
        var minimum = locale.Number(insights.MinimumGroupSize);
        var month = insights.Period is { } period ? locale.MonthAndYear(period) : string.Empty;
        var isReady = insights.Period is not null && !insights.IsAnonymized;

        string? notice = insights switch
        {
            { Period: null } => texts.NoClosedMonthsToAnalyze,
            { IsAnonymized: true } => texts.InsightsAnonymized(month, minimum),
            { Concerns.Count: 0 } => texts.NoConcerns(month, minimum),
            _ => null,
        };

        var items = notice is not null
            ? Array.Empty<PdfItem>()
            : insights.Concerns
                .Select((concern, index) =>
                {
                    var concernTexts = texts.Concerns(concern.Concern);

                    return new PdfItem(
                        locale.Number(index + 1) + ". " + concernTexts.Title,
                        concernTexts.Description,
                        locale.Number(concern.Percentage, 1) + "%",
                        texts.AffectedOf(
                            locale.Number(concern.AffectedEmployees),
                            locale.Number(concern.EvaluatedEmployees),
                            concernTexts.Evaluated(concern.EvaluatedEmployees == 1)),
                        PdfTone.Neutral);
                })
                .ToArray();

        return new PdfSection(
            texts.ConcernsTitle,
            isReady && insights.MeasuredEmployees is { } measured
                ? texts.Measured(month, locale.Number(measured), measured == 1)
                : texts.ConcernsDescription,
            notice,
            items.Length == 0 ? Array.Empty<PdfItemGroup>() : new[] { new PdfItemGroup(null, items) },
            isReady ? texts.ConcernsFooter(minimum) : null);
    }

    private static PdfSection Recommendations(CompanyInsightsResponse insights, CompanyReportTexts texts)
    {
        var locale = texts.Locale;
        var minimum = locale.Number(insights.MinimumGroupSize);
        var month = insights.Period is { } period ? locale.MonthAndYear(period) : string.Empty;
        var isReady = insights.Period is not null && !insights.IsAnonymized && insights.RecommendationsAvailable;

        string? notice = insights switch
        {
            { Period: null } => texts.NoClosedMonthsToAnalyze,
            { IsAnonymized: true } => texts.InsightsAnonymized(month, minimum),
            { RecommendationsAvailable: false } => texts.RecommendationsUnavailable,
            _ => null,
        };

        var items = notice is not null
            ? Array.Empty<PdfItem>()
            : insights.Recommendations
                .Select(recommendation => new PdfItem(
                    recommendation.Title,
                    recommendation.Body,
                    texts.RecommendationKinds(recommendation.Kind),
                    null,
                    recommendation.Kind switch
                    {
                        RecommendationKind.Positive => PdfTone.Positive,
                        RecommendationKind.Warning => PdfTone.Warning,
                        _ => PdfTone.Tip,
                    }))
                .ToArray();

        return new PdfSection(
            texts.RecommendationsTitle,
            texts.RecommendationsDescription,
            notice,
            items.Length == 0 ? Array.Empty<PdfItemGroup>() : new[] { new PdfItemGroup(null, items) },
            isReady ? texts.RecommendationsFooter(month) : null);
    }

    /// <summary>El front pone en mayúscula la primera letra del período del encabezado (<c>first-letter:uppercase</c>).</summary>
    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
