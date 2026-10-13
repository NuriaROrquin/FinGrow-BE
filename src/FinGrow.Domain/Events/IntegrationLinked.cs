namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

public sealed record IntegrationLinked(
    Guid IntegrationId,
    Guid EmployeeId,
    IntegrationProvider Provider,
    DateTimeOffset OccurredAt) : IDomainEvent;
