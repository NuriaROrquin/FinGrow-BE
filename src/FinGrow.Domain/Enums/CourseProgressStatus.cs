namespace FinGrow.Domain.Enums;

/// <summary>En que punto esta un empleado con un curso. Se deriva de las lecciones que termino.</summary>
public enum CourseProgressStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3
}
