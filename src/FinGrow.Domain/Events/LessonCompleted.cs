namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;

public sealed record LessonCompleted(Guid EmployeeId, Guid LessonId, DateTimeOffset OccurredAt) : IDomainEvent;
