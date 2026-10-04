namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Enums;

public sealed record CourseProgress(int CompletedLessons, int LessonCount)
{
    public int Percentage => LessonCount == 0 ? 0 : CompletedLessons * 100 / LessonCount;

    public CourseProgressStatus Status => CompletedLessons == 0
        ? CourseProgressStatus.NotStarted
        : CompletedLessons == LessonCount
            ? CourseProgressStatus.Completed
            : CourseProgressStatus.InProgress;

    public bool IsCompleted => Status == CourseProgressStatus.Completed;
}
