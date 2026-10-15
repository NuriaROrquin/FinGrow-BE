namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

public sealed record BudgetLimitSet(
    Guid BudgetId,
    Guid EmployeeId,
    ExpenseCategory Category,
    DateTimeOffset OccurredAt) : IDomainEvent;
