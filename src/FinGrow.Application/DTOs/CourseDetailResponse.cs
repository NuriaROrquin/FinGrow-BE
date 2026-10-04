namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record CourseDetailResponse(
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
    CourseProgressStatus ProgressStatus,
    Guid? ResumeLessonId,
    IReadOnlyList<LessonResponse> Lessons)
{
    public static CourseDetailResponse FromEntity(Course course, IReadOnlySet<Guid> completedLessonIds)
    {
        var progress = course.ProgressFor(completedLessonIds);

        return new CourseDetailResponse(
            course.Id,
            course.Slug,
            course.Title,
            course.Description,
            course.Level,
            course.Category,
            course.RelatedInvestmentType,
            course.DurationMinutes,
            progress.LessonCount,
            progress.CompletedLessons,
            progress.Percentage,
            progress.Status,
            course.ResumeLessonFor(completedLessonIds)?.Id,
            course.Lessons
                .Select(lesson => LessonResponse.FromEntity(lesson, completedLessonIds.Contains(lesson.Id)))
                .ToList());
    }
}
