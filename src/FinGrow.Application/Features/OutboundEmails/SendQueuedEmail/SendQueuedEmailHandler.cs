namespace FinGrow.Application.Features.OutboundEmails.SendQueuedEmail;

using FinGrow.Application.Events;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class SendQueuedEmailHandler(IOutboundEmailRepository emails, OutboundEmailDispatcher dispatcher)
    : INotificationHandler<DomainEventEnvelope<OutboundEmailQueued>>
{
    public async Task Handle(DomainEventEnvelope<OutboundEmailQueued> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var email = await emails.GetByIdAsync(notification.DomainEvent.OutboundEmailId, cancellationToken);

        if (email is { Status: OutboundEmailStatus.Pending })
        {
            await dispatcher.DispatchAsync(email, cancellationToken);
        }
    }
}
