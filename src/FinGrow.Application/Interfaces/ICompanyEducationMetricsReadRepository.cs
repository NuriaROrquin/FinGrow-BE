namespace FinGrow.Application.Interfaces;

using FinGrow.Domain.ValueObjects;

public interface ICompanyEducationMetricsReadRepository
{
    Task<CompanyEducationMetrics> GetAsync(
        Guid companyId,
        DateOnly last30DaysStart,
        CancellationToken cancellationToken = default);

    Task<int> CountCompletedCoursesAsync(
        Guid companyId,
        MetricsPeriod period,
        CancellationToken cancellationToken = default);
}

public sealed record CompanyEducationMetrics(
    int TotalCompletedCourses,
    int Last30DaysCompletedCourses,
    int CourseRatingsCount,
    int CourseRatingSum);
