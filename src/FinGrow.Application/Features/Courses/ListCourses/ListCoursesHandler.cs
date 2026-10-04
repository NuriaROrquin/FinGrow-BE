namespace FinGrow.Application.Features.Courses.ListCourses;

using Common;
using DTOs;
using Domain.Repositories;
using MediatR;

internal sealed class ListCoursesHandler(ICourseRepository courseRepository)
    : IRequestHandler<ListCoursesQuery, Result<IReadOnlyList<CourseSummaryResponse>>>
{
    public async Task<Result<IReadOnlyList<CourseSummaryResponse>>> Handle(
        ListCoursesQuery request,
        CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListPublishedAsync(
            request.Level,
            request.MaxDurationMinutes,
            cancellationToken);

        var completedLessonIds = await courseRepository.ListCompletedLessonIdsAsync(request.EmployeeId, cancellationToken);

        var summaries = courses
            .Select(course => CourseSummaryResponse.FromEntity(course, completedLessonIds))
            .Where(summary => request.ProgressStatus is null || summary.ProgressStatus == request.ProgressStatus)
            .ToList();

        return Result.Success<IReadOnlyList<CourseSummaryResponse>>(summaries);
    }
}
