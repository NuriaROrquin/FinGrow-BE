namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

public sealed record TransactionConfirmed(
    Guid TransactionId,
    Guid EmployeeId,
    ExpenseCategory? ExpenseCategory,
    decimal Amount,
    Currency Currency,
    DateOnly OccurredOn,
    DateTimeOffset OccurredAt) : IDomainEvent;
