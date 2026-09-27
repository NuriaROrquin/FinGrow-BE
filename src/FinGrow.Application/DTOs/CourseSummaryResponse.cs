namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

/// <summary>
/// Un curso en el catalogo, con el avance del empleado que lo consulta. Las lecciones no viajan:
/// se piden al abrir el curso.
/// </summary>
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

        // Se redondea para abajo: un curso no puede mostrar 100% si le falta una leccion.
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
