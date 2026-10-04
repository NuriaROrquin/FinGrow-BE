namespace FinGrow.Application.Events;

using FinGrow.Domain.Common;
using MediatR;

public sealed record DomainEventEnvelope<TEvent>(TEvent DomainEvent) : INotification
    where TEvent : IDomainEvent;
