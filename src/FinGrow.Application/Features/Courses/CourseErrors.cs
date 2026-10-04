namespace FinGrow.Application.Features.Courses;

using Common;

internal static class CourseErrors
{
    public static Error NotFound(string slug) =>
        Error.NotFound("Course.NotFound", $"No existe un curso publicado con slug '{slug}'.");

    public static Error LessonNotFound(Guid lessonId) =>
        Error.NotFound("Course.LessonNotFound", $"No existe una leccion con id '{lessonId}' en este curso.");

    public static Error NotCompleted() =>
        Error.Conflict("Course.NotCompleted", "Solo se puede calificar un curso terminado.");
}
