namespace FinGrow.Domain.Entities;

using FinGrow.Domain.Common;
using FinGrow.Domain.Events;

public sealed class LessonCompletion : AggregateRoot
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

        Raise(new LessonCompleted(employeeId, lessonId, completedAt));
    }

    public Guid EmployeeId { get; private set; }

    public Guid LessonId { get; private set; }

    public DateTimeOffset CompletedAt { get; private set; }

    public static LessonCompletion Create(Guid employeeId, Guid lessonId, DateTimeOffset completedAt) =>
        new(Guid.CreateVersion7(), employeeId, lessonId, completedAt);
}
