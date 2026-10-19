namespace FinGrow.Application.Features.Metrics.GetCompanyMetricsComparison;

public sealed record CompanyMetricsComparisonResponse(
    IReadOnlyList<CompanyMetricsComparisonPeriodResponse> Periods,
    decimal? ParticipationRateChange,
    decimal? AverageFinancialHealthScoreChange);

public sealed record CompanyMetricsComparisonPeriodResponse(
    DateOnly Period,
    decimal? ParticipationRate,
    decimal? AverageFinancialHealthScore);
