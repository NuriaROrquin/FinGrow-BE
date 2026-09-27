namespace FinGrow.Domain.Repositories;

using Entities;
using Enums;

public interface ICourseRepository
{
    /// <summary>
    /// Los cursos publicados, con sus lecciones, de nivel mas basico a mas avanzado. Sin filtro de
    /// nivel o de duracion devuelve todo el catalogo publicado.
    /// </summary>
    /// <param name="maxDurationMinutes">Solo los cursos que duran ese tiempo o menos.</param>
    Task<IReadOnlyList<Course>> ListPublishedAsync(
        CourseLevel? level,
        int? maxDurationMinutes,
        CancellationToken cancellationToken = default);

    /// <summary>Los ids de las lecciones que el empleado ya termino.</summary>
    Task<IReadOnlySet<Guid>> ListCompletedLessonIdsAsync(Guid employeeId, CancellationToken cancellationToken = default);
}
