namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

public sealed record InvestmentAdded(
    Guid InvestmentId,
    Guid EmployeeId,
    InvestmentSource Source,
    DateTimeOffset OccurredAt) : IDomainEvent;
