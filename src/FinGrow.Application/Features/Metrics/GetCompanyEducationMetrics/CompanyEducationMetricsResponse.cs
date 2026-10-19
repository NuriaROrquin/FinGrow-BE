namespace FinGrow.Application.Features.Metrics.GetCompanyEducationMetrics;

public sealed record CompanyEducationMetricsResponse(
    int TotalCompletedCourses,
    decimal? SatisfactionPercentage,
    int Last30DaysCompletedCourses);
