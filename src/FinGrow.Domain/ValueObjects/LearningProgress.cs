namespace FinGrow.Domain.ValueObjects;

using FinGrow.Domain.Enums;

public sealed record LearningProgress(
    int CourseCount,
    int CompletedCourses,
    int InProgressCourses,
    int CompletedLessons,
    int LessonCount)
{
    public int NotStartedCourses => CourseCount - CompletedCourses - InProgressCourses;

    // Se mide en lecciones y no promediando los cursos: un curso de dos lecciones no pesa lo
    // mismo que uno de quince.
    public int Percentage => LessonCount == 0 ? 0 : CompletedLessons * 100 / LessonCount;

    public static LearningProgress From(IReadOnlyCollection<CourseProgress> courses) =>
        new(courses.Count,
            courses.Count(course => course.Status == CourseProgressStatus.Completed),
            courses.Count(course => course.Status == CourseProgressStatus.InProgress),
            courses.Sum(course => course.CompletedLessons),
            courses.Sum(course => course.LessonCount));
}
