namespace FinGrow.Application.Features.Metrics.GetCompanyInsights;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;

/// <summary>
/// Preocupaciones (HU-61) y recomendaciones (HU-64) del último mes fotografiado. Sin foto todavía,
/// <c>Period</c> viaja en <c>null</c>; con menos empleados medidos que <c>MinimumGroupSize</c>, el
/// mes viaja con <c>IsAnonymized</c> y sin preocupaciones ni recomendaciones.
/// </summary>
public sealed record CompanyInsightsResponse(
    DateOnly? Period,
    bool IsAnonymized,
    int MinimumGroupSize,
    int? MeasuredEmployees,
    IReadOnlyList<CompanyConcernResponse> Concerns,
    IReadOnlyList<FinancialRecommendation> Recommendations,
    bool RecommendationsAvailable)
{
    public static CompanyInsightsResponse WithoutPeriod(int minimumGroupSize) =>
        new(null, false, minimumGroupSize, null, Array.Empty<CompanyConcernResponse>(), Array.Empty<FinancialRecommendation>(), false);

    public static CompanyInsightsResponse Anonymized(DateOnly period, int minimumGroupSize) =>
        new(period, true, minimumGroupSize, null, Array.Empty<CompanyConcernResponse>(), Array.Empty<FinancialRecommendation>(), false);
}

public sealed record CompanyConcernResponse(
    CompanyConcern Concern,
    int AffectedEmployees,
    int EvaluatedEmployees,
    decimal Percentage)
{
    public static CompanyConcernResponse From(CompanyConcernSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new CompanyConcernResponse(
            summary.Concern,
            summary.AffectedEmployees,
            summary.EvaluatedEmployees,
            summary.Percentage);
    }
}
