namespace FinGrow.Application.Interfaces;

public interface ICompanyEducationMetricsReadRepository
{
    Task<CompanyEducationMetrics> GetAsync(
        Guid companyId,
        DateOnly last30DaysStart,
        CancellationToken cancellationToken = default);
}

public sealed record CompanyEducationMetrics(
    int TotalCompletedCourses,
    int Last30DaysCompletedCourses,
    int CourseRatingsCount,
    int CourseRatingSum);
