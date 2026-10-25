namespace FinGrow.Application.Features.Metrics.Trends;

public sealed record CompanyTrend(
    CompanyTrendIndicator Indicator,
    CompanyTrendUnit Unit,
    decimal PreviousValue,
    decimal CurrentValue,
    decimal Change);

public sealed record CompanyTrends(
    IReadOnlyList<CompanyTrend> Positive,
    IReadOnlyList<CompanyTrend> Opportunities);
