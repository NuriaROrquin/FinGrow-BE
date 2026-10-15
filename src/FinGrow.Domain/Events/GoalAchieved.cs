namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;

public sealed record GoalAchieved(Guid GoalId, Guid EmployeeId, DateTimeOffset OccurredAt) : IDomainEvent;
