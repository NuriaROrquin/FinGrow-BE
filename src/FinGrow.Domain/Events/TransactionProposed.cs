namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

public sealed record TransactionProposed(
    Guid TransactionId,
    Guid EmployeeId,
    TransactionSource Source,
    DateTimeOffset OccurredAt) : IDomainEvent;
