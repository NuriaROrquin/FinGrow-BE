namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;

public sealed record GoalCreated(Guid GoalId, Guid EmployeeId, DateTimeOffset OccurredAt) : IDomainEvent;
