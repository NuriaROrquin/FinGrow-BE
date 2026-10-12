namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

public sealed record TransactionConfirmed(
    Guid TransactionId,
    Guid EmployeeId,
    decimal Amount,
    Currency Currency,
    ExpenseCategory? ExpenseCategory,
    DateOnly OccurredOn,
    DateTimeOffset OccurredAt) : IDomainEvent;
