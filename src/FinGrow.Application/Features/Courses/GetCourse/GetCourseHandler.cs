namespace FinGrow.Application.Features.Courses.GetCourse;

using Common;
using DTOs;
using Interfaces;
using Domain.Repositories;
using MediatR;

internal sealed class GetCourseHandler(
    ICourseRepository courseRepository,
    ICourseRatingReadRepository ratingReadRepository)
    : IRequestHandler<GetCourseQuery, Result<CourseDetailResponse>>
{
    public async Task<Result<CourseDetailResponse>> Handle(GetCourseQuery request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);

        if (course is null)
        {
            return Result.Failure<CourseDetailResponse>(CourseErrors.NotFound(request.Slug));
        }

        var completedLessonIds = await courseRepository.ListCompletedLessonIdsAsync(request.EmployeeId, cancellationToken);

        var ratings = await ratingReadRepository.GetSummariesAsync(
            request.EmployeeId,
            new[] { course.Id },
            cancellationToken);

        return Result.Success(CourseDetailResponse.FromEntity(
            course,
            completedLessonIds,
            ratings.GetValueOrDefault(course.Id, CourseRatingSummary.None)));
    }
}
