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
        var progress = course.ProgressFor(completedLessonIds);

        return new CourseSummaryResponse(
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
            progress.Status);
    }
}
