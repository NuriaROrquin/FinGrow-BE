namespace FinGrow.Application.Features.Courses.GetCourse;

using Common;
using DTOs;
using Domain.Repositories;
using MediatR;

internal sealed class GetCourseHandler(ICourseRepository courseRepository)
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

        return Result.Success(CourseDetailResponse.FromEntity(course, completedLessonIds));
    }
}
