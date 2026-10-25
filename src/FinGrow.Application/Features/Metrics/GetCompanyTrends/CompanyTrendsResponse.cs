namespace FinGrow.Application.Features.Metrics.GetCompanyTrends;

using FinGrow.Application.Features.Metrics.Trends;

public sealed record CompanyTrendsResponse(
    DateOnly? Period,
    DateOnly? PreviousPeriod,
    bool IsAnonymized,
    int MinimumGroupSize,
    IReadOnlyList<CompanyTrend> PositiveTrends,
    IReadOnlyList<CompanyTrend> Opportunities)
{
    public static CompanyTrendsResponse WithoutPeriod(int minimumGroupSize) =>
        new(null, null, false, minimumGroupSize, Array.Empty<CompanyTrend>(), Array.Empty<CompanyTrend>());

    public static CompanyTrendsResponse WithoutPreviousPeriod(DateOnly period, int minimumGroupSize) =>
        new(period, null, false, minimumGroupSize, Array.Empty<CompanyTrend>(), Array.Empty<CompanyTrend>());

    public static CompanyTrendsResponse Anonymized(DateOnly period, DateOnly previousPeriod, int minimumGroupSize) =>
        new(period, previousPeriod, true, minimumGroupSize, Array.Empty<CompanyTrend>(), Array.Empty<CompanyTrend>());
}
