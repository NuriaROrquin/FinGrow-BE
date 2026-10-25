namespace FinGrow.Application.Features.Metrics.GetCompanyInsights;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

/// <summary>
/// Único lugar donde se arma lo que viaja a FinGrow-AI sobre una empresa. Recibe solo lo que la
/// empresa ya puede ver: las preocupaciones que pasaron el umbral de anonimato, el promedio que
/// devolvió la política y la participación del mes.
/// </summary>
internal static class CompanyInsightsInputBuilder
{
    public static CompanyRecommendationsInput Build(
        Language language,
        DateOnly period,
        int measuredEmployees,
        CompanyMetricsSnapshot? snapshot,
        MetricsAnonymizationPolicy anonymizationPolicy,
        IReadOnlyList<CompanyConcernSummary> concerns)
    {
        ArgumentNullException.ThrowIfNull(anonymizationPolicy);
        ArgumentNullException.ThrowIfNull(concerns);

        var metrics = snapshot is not null && anonymizationPolicy.CanRevealCompany(snapshot.Metrics)
            ? snapshot.Metrics
            : null;

        return new CompanyRecommendationsInput(
            language,
            period.Year,
            period.Month,
            measuredEmployees,
            metrics is null ? null : anonymizationPolicy.CompanyAverageScore(metrics),
            metrics?.ParticipationRate,
            concerns);
    }
}
