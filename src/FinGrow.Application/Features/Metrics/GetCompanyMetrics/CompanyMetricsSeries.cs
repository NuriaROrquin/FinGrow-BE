namespace FinGrow.Application.Features.Metrics.GetCompanyMetrics;

using FinGrow.Domain.Entities;

/// <summary>
/// Los meses con foto de una empresa tal como los ve el panel: los que no llegan al umbral de
/// anonimato van marcados y sin valores. La usan <c>GET /api/company/metrics</c> y los gráficos de
/// evolución del PDF del reporte, así los dos muestran los mismos puntos.
/// </summary>
internal static class CompanyMetricsSeries
{
    public static IReadOnlyList<CompanyMetricsResponse> Build(
        IEnumerable<CompanyMetricsSnapshot> snapshots,
        int fiscalYearStartMonth,
        MetricsAnonymizationPolicy anonymizationPolicy)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(anonymizationPolicy);

        return snapshots
            .OrderBy(snapshot => snapshot.PeriodStart)
            .Select(snapshot =>
            {
                var isHidden = !anonymizationPolicy.CanRevealCompany(snapshot.Metrics);

                return CompanyMetricsResponse.FromSnapshot(
                    snapshot,
                    fiscalYearStartMonth,
                    anonymizationPolicy.CompanyAverageScore(snapshot.Metrics),
                    isHidden,
                    anonymizationPolicy.CompanyMinimumEmployees,
                    isHidden ? MetricsAnonymizationPolicy.NotEnoughEmployees.Description : null);
            })
            .ToList();
    }
}
