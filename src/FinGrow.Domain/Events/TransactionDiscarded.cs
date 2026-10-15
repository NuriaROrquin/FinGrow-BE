namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;

public sealed record TransactionDiscarded(
    Guid TransactionId,
    Guid EmployeeId,
    DateTimeOffset OccurredAt) : IDomainEvent;
