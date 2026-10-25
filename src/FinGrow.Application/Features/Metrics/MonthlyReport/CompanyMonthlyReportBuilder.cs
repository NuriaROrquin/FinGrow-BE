namespace FinGrow.Application.Features.Metrics.MonthlyReport;

using FinGrow.Application.Features.Metrics.GetCompanyTrends;
using FinGrow.Application.Features.Metrics.Trends;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;

/// <summary>
/// Único lugar donde se arma el reporte mensual de una empresa: lo exporta el panel (HU-65) y lo
/// manda el mail mensual (HU-82). Lee solo las fotos de <c>metrics-snapshot</c>, los puntajes y los
/// cursos del mes, y todo lo que muestra pasa antes por <see cref="CompanyMonthIndicators"/> y
/// <see cref="MetricsAnonymizationPolicy"/>, igual que la tarjeta de tendencias.
/// </summary>
internal sealed class CompanyMonthlyReportBuilder(
    IMetricsSnapshotRepository snapshots,
    IFinancialHealthScoreRepository scores,
    ICompanyEducationMetricsReadRepository education,
    MetricsAnonymizationPolicy anonymizationPolicy)
{
    /// <summary>Los indicadores que pide HU-82, en el orden en que se muestran.</summary>
    private static readonly CompanyTrendIndicator[] HeadlineIndicators =
    {
        CompanyTrendIndicator.ParticipationRate,
        CompanyTrendIndicator.AverageFinancialHealthScore,
        CompanyTrendIndicator.CompletedCourses,
    };

    /// <summary>
    /// Arma el reporte del mes pedido contra el mes inmediatamente anterior. Devuelve <c>null</c> si
    /// ese mes todavía no tiene foto.
    /// </summary>
    public async Task<CompanyMonthlyReportResponse?> BuildAsync(
        Company company,
        MetricsPeriod period,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(period);

        var current = await snapshots.FindCompanyAsync(company.Id, period.Start, cancellationToken);

        if (current is null)
        {
            return null;
        }

        var previousPeriod = period.Previous();
        var previous = await snapshots.FindCompanyAsync(company.Id, previousPeriod.Start, cancellationToken);

        var currentMonth = new Month(current, await ReadAsync(company.Id, current, cancellationToken));
        var previousMonth = previous is null
            ? null
            : new Month(previous, await ReadAsync(company.Id, previous, cancellationToken));

        var minimumGroupSize = anonymizationPolicy.CompanyMinimumEmployees;
        var isAnonymized = !anonymizationPolicy.CanRevealCompany(current.Metrics);
        var averageScore = currentMonth.ValueOf(CompanyTrendIndicator.AverageFinancialHealthScore);

        return new CompanyMonthlyReportResponse(
            company.Name,
            period.Start,
            previousPeriod.Start,
            minimumGroupSize,
            isAnonymized,
            current.Metrics.ActiveEmployees,
            isAnonymized ? null : current.Metrics.ParticipatingEmployees,
            averageScore is { } score ? FinancialHealthScale.BandOf(score) : null,
            HeadlineIndicators
                .Select(indicator => IndicatorOf(indicator, currentMonth, previousMonth))
                .ToList(),
            TrendsOf(period, previousPeriod, currentMonth, previousMonth, minimumGroupSize));
    }

    private CompanyReportIndicatorResponse IndicatorOf(
        CompanyTrendIndicator indicator,
        Month current,
        Month? previous)
    {
        var value = current.ValueOf(indicator);
        var previousValue = previous?.ValueOf(indicator);

        return new CompanyReportIndicatorResponse(
            indicator,
            CompanyTrendsCalculator.UnitOf(indicator),
            StatusOf(indicator, current),
            value,
            previous is null ? CompanyReportValueStatus.NoData : StatusOf(indicator, previous),
            previousValue,
            value is { } currentAmount && previousValue is { } previousAmount
                ? decimal.Round(currentAmount - previousAmount, 2, MidpointRounding.ToEven)
                : null);
    }

    /// <summary>
    /// <see cref="CompanyMonthIndicators"/> deja afuera todo el mes si no llega al umbral de
    /// empleados activos. Si llega y aun así falta un indicador, solo puede ser el puntaje
    /// promedio, que tiene su propio conteo de empleados medidos.
    /// </summary>
    private CompanyReportValueStatus StatusOf(CompanyTrendIndicator indicator, Month month)
    {
        if (month.Indicators.ContainsKey(indicator))
        {
            return CompanyReportValueStatus.Visible;
        }

        return anonymizationPolicy.CanRevealCompany(month.Snapshot.Metrics)
            ? CompanyReportValueStatus.TooFewScoredEmployees
            : CompanyReportValueStatus.TooFewActiveEmployees;
    }

    /// <summary>Las mismas reglas que <c>GET /api/company/metrics/trends</c> (HU-63), para el mes del reporte.</summary>
    private CompanyTrendsResponse TrendsOf(
        MetricsPeriod period,
        MetricsPeriod previousPeriod,
        Month current,
        Month? previous,
        int minimumGroupSize)
    {
        if (previous is null)
        {
            return CompanyTrendsResponse.WithoutPreviousPeriod(period.Start, minimumGroupSize);
        }

        if (!anonymizationPolicy.CanRevealCompany(current.Snapshot.Metrics)
            || !anonymizationPolicy.CanRevealCompany(previous.Snapshot.Metrics))
        {
            return CompanyTrendsResponse.Anonymized(period.Start, previousPeriod.Start, minimumGroupSize);
        }

        var trends = CompanyTrendsCalculator.Compare(previous.Indicators, current.Indicators);

        return new CompanyTrendsResponse(
            period.Start,
            previousPeriod.Start,
            false,
            minimumGroupSize,
            trends.Positive,
            trends.Opportunities);
    }

    private async Task<IReadOnlyDictionary<CompanyTrendIndicator, decimal>> ReadAsync(
        Guid companyId,
        CompanyMetricsSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var monthScores = await scores.ListByCompanyAsync(companyId, snapshot.PeriodStart, cancellationToken);
        var completedCourses = await education.CountCompletedCoursesAsync(companyId, snapshot.Period, cancellationToken);

        return CompanyMonthIndicators.Read(
            snapshot.Metrics,
            monthScores.Select(score => score.Factors).ToList(),
            completedCourses,
            anonymizationPolicy);
    }

    private sealed record Month(
        CompanyMetricsSnapshot Snapshot,
        IReadOnlyDictionary<CompanyTrendIndicator, decimal> Indicators)
    {
        public decimal? ValueOf(CompanyTrendIndicator indicator) =>
            Indicators.TryGetValue(indicator, out var value) ? value : null;
    }
}
