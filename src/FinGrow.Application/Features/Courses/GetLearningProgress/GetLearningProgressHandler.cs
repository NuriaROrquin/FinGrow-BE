namespace FinGrow.Application.Features.Courses.GetLearningProgress;

using Common;
using DTOs;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

internal sealed class GetLearningProgressHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetLearningProgressQuery, Result<LearningProgressResponse>>
{
    public async Task<Result<LearningProgressResponse>> Handle(
        GetLearningProgressQuery request,
        CancellationToken cancellationToken)
    {
        var courses = await courseRepository.ListPublishedAsync(
            level: null,
            maxDurationMinutes: null,
            cancellationToken);

        var completedLessonIds = await courseRepository.ListCompletedLessonIdsAsync(request.EmployeeId, cancellationToken);

        var progress = LearningProgress.From(courses
            .Select(course => course.ProgressFor(completedLessonIds))
            .ToList());

        return Result.Success(LearningProgressResponse.From(progress));
    }
}
