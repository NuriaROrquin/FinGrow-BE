namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;

/// <summary>
/// Que un empleado termino una leccion. Es lo que mide el progreso de cada curso (HU-36): el
/// catalogo es de todos, el avance es de cada uno. Marcar una leccion como vista lo hace HU-37.
/// </summary>
public sealed class LessonCompletion : Entity
{
    private LessonCompletion()
    {
    }

    private LessonCompletion(Guid id, Guid employeeId, Guid lessonId, DateTimeOffset completedAt)
        : base(id)
    {
        EmployeeId = employeeId;
        LessonId = lessonId;
        CompletedAt = completedAt;
    }

    public Guid EmployeeId { get; private set; }

    public Guid LessonId { get; private set; }

    public DateTimeOffset CompletedAt { get; private set; }

    public static LessonCompletion Create(Guid employeeId, Guid lessonId, DateTimeOffset completedAt) =>
        new(Guid.CreateVersion7(), employeeId, lessonId, completedAt);
}
