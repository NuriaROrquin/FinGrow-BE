namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;

public sealed record OutboundEmailQueued(Guid OutboundEmailId, DateTimeOffset OccurredAt) : IDomainEvent;
