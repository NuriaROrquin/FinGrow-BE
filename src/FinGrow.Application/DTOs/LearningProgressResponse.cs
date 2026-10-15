namespace FinGrow.Application.DTOs;

using Domain.ValueObjects;

public sealed record LearningProgressResponse(
    int CourseCount,
    int CompletedCourses,
    int InProgressCourses,
    int NotStartedCourses,
    int LessonCount,
    int CompletedLessons,
    int ProgressPercentage)
{
    public static LearningProgressResponse From(LearningProgress progress) =>
        new(progress.CourseCount,
            progress.CompletedCourses,
            progress.InProgressCourses,
            progress.NotStartedCourses,
            progress.LessonCount,
            progress.CompletedLessons,
            progress.Percentage);
}
