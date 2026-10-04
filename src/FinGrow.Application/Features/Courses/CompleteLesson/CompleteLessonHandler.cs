namespace FinGrow.Application.Features.Courses.CompleteLesson;

using Common;
using DTOs;
using Interfaces;
using Domain.Entities;
using Domain.Repositories;
using MediatR;

internal sealed class CompleteLessonHandler(
    ICourseRepository courseRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CompleteLessonCommand, Result<CourseDetailResponse>>
{
    public async Task<Result<CourseDetailResponse>> Handle(CompleteLessonCommand request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);

        if (course is null)
        {
            return Result.Failure<CourseDetailResponse>(CourseErrors.NotFound(request.Slug));
        }

        var lesson = course.FindLesson(request.LessonId);

        if (lesson is null)
        {
            return Result.Failure<CourseDetailResponse>(CourseErrors.LessonNotFound(request.LessonId));
        }

        var completedLessonIds = (await courseRepository.ListCompletedLessonIdsAsync(request.EmployeeId, cancellationToken))
            .ToHashSet();

        if (completedLessonIds.Add(lesson.Id))
        {
            courseRepository.AddCompletion(LessonCompletion.Create(request.EmployeeId, lesson.Id, dateTimeProvider.UtcNow));
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(CourseDetailResponse.FromEntity(course, completedLessonIds));
    }
}
