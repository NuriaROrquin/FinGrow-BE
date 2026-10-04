namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record CourseSummaryResponse(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    CourseLevel Level,
    EducationCategory Category,
    InvestmentType? RelatedInvestmentType,
    int DurationMinutes,
    int LessonCount,
    int CompletedLessons,
    int ProgressPercentage,
    CourseProgressStatus ProgressStatus)
{
    public static CourseSummaryResponse FromEntity(Course course, IReadOnlySet<Guid> completedLessonIds)
    {
        var lessonCount = course.Lessons.Count;
        var completedLessons = course.Lessons.Count(lesson => completedLessonIds.Contains(lesson.Id));

        var progressPercentage = lessonCount == 0 ? 0 : completedLessons * 100 / lessonCount;

        var status = completedLessons == 0
            ? CourseProgressStatus.NotStarted
            : completedLessons == lessonCount
                ? CourseProgressStatus.Completed
                : CourseProgressStatus.InProgress;

        return new CourseSummaryResponse(
            course.Id,
            course.Slug,
            course.Title,
            course.Description,
            course.Level,
            course.Category,
            course.RelatedInvestmentType,
            course.DurationMinutes,
            lessonCount,
            completedLessons,
            progressPercentage,
            status);
    }
}
