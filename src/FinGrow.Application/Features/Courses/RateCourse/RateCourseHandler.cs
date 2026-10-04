namespace FinGrow.Application.Features.Courses.RateCourse;

using Common;
using DTOs;
using Interfaces;
using Domain.Entities;
using Domain.Repositories;
using MediatR;

internal sealed class RateCourseHandler(
    ICourseRepository courseRepository,
    ICourseRatingRepository ratingRepository,
    ICourseRatingReadRepository ratingReadRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<RateCourseCommand, Result<CourseDetailResponse>>
{
    public async Task<Result<CourseDetailResponse>> Handle(RateCourseCommand request, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetPublishedBySlugAsync(request.Slug, cancellationToken);

        if (course is null)
        {
            return Result.Failure<CourseDetailResponse>(CourseErrors.NotFound(request.Slug));
        }

        var completedLessonIds = await courseRepository.ListCompletedLessonIdsAsync(request.EmployeeId, cancellationToken);

        if (!course.ProgressFor(completedLessonIds).IsCompleted)
        {
            return Result.Failure<CourseDetailResponse>(CourseErrors.NotCompleted());
        }

        var rating = await ratingRepository.FindAsync(request.EmployeeId, course.Id, cancellationToken);

        if (rating is null)
        {
            ratingRepository.Add(CourseRating.Create(request.EmployeeId, course.Id, request.Score, dateTimeProvider.UtcNow));
        }
        else
        {
            rating.ChangeScore(request.Score, dateTimeProvider.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

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
