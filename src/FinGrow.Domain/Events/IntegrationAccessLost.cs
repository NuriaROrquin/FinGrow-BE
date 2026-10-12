namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

public sealed record IntegrationAccessLost(
    Guid IntegrationId,
    Guid EmployeeId,
    IntegrationProvider Provider,
    DateTimeOffset OccurredAt) : IDomainEvent;
