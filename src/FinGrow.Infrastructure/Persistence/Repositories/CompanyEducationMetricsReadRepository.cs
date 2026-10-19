namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Common;
using Microsoft.EntityFrameworkCore;

internal sealed class CompanyEducationMetricsReadRepository(FinGrowDbContext dbContext)
    : ICompanyEducationMetricsReadRepository
{
    public async Task<CompanyEducationMetrics> GetAsync(
        Guid companyId,
        DateOnly last30DaysStart,
        CancellationToken cancellationToken = default)
    {
        var last30Days = ArgentinaTime.StartOfDay(last30DaysStart);

        var completedCoursesQuery = dbContext.LessonCompletions
            .Join(
                dbContext.Employees.Where(employee => employee.CompanyId == companyId),
                completion => completion.EmployeeId,
                employee => employee.Id,
                (completion, _) => completion)
            .Join(
                dbContext.Lessons,
                completion => completion.LessonId,
                lesson => lesson.Id,
                (completion, lesson) => new
                {
                    completion.EmployeeId,
                    CourseId = lesson.CourseId,
                    completion.CompletedAt,
                })
            .Join(
                dbContext.Courses.Where(course => course.PublishedAt != null),
                completion => completion.CourseId,
                course => course.Id,
                (completion, _) => completion)
            .GroupBy(completion => new { completion.EmployeeId, completion.CourseId })
            .Where(group =>
                group.Count() == dbContext.Lessons.Count(
                    lesson => lesson.CourseId == group.Key.CourseId))
            .Select(group => group.Max(completion => completion.CompletedAt));

        var totalCompletedCourses = await completedCoursesQuery.CountAsync(cancellationToken);
        var last30DaysCompletedCourses = await completedCoursesQuery
            .Where(completedAt => completedAt >= last30Days)
            .CountAsync(cancellationToken);

        var ratingTotals = await dbContext.CourseRatings
            .Join(
                dbContext.Employees.Where(employee => employee.CompanyId == companyId),
                rating => rating.EmployeeId,
                employee => employee.Id,
                (rating, _) => rating)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Count = group.Count(),
                Sum = group.Sum(rating => rating.Score),
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new CompanyEducationMetrics(
            totalCompletedCourses,
            last30DaysCompletedCourses,
            ratingTotals?.Count ?? 0,
            ratingTotals?.Sum ?? 0);
    }
}
