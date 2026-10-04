namespace FinGrow.Application.Features.Courses.ListCourses;

using Common;
using DTOs;
using Interfaces;
using Domain.Repositories;
using MediatR;

internal sealed class ListCoursesHandler(
    ICourseRepository courseRepository,
    ICourseRatingReadRepository ratingReadRepository)
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

        var ratings = await ratingReadRepository.GetSummariesAsync(
            request.EmployeeId,
            courses.Select(course => course.Id).ToList(),
            cancellationToken);

        var summaries = courses
            .Select(course => CourseSummaryResponse.FromEntity(
                course,
                completedLessonIds,
                ratings.GetValueOrDefault(course.Id, CourseRatingSummary.None)))
            .Where(summary => request.ProgressStatus is null || summary.ProgressStatus == request.ProgressStatus)
            .ToList();

        return Result.Success<IReadOnlyList<CourseSummaryResponse>>(summaries);
    }
}
