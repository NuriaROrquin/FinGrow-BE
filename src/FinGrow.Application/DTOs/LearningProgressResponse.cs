namespace FinGrow.Application.DTOs;

using Domain.ValueObjects;

public sealed record LearningProgressResponse(
    int CourseCount,
    int CompletedCourses,
    int InProgressCourses,
    int NotStartedCourses,
    int LessonCount,
    int CompletedLessons,
    int ProgressPercentage,
    int StreakWeeks,
    bool StudiedThisWeek)
{
    public static LearningProgressResponse From(LearningProgress progress, LearningStreak streak) =>
        new(progress.CourseCount,
            progress.CompletedCourses,
            progress.InProgressCourses,
            progress.NotStartedCourses,
            progress.LessonCount,
            progress.CompletedLessons,
            progress.Percentage,
            streak.Weeks,
            streak.StudiedThisWeek);
}
