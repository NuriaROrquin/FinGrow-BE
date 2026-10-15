namespace FinGrow.Application.Features.Receipts.ReleaseDiscarded;

using FinGrow.Application.Events;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Events;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ReleaseDiscardedReceiptHandler(
    ITransactionReceiptRepository receipts,
    IUnitOfWork unitOfWork)
    : INotificationHandler<DomainEventEnvelope<TransactionDiscarded>>
{
    public async Task Handle(DomainEventEnvelope<TransactionDiscarded> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var receipt = await receipts.GetByTransactionIdAsync(notification.DomainEvent.TransactionId, cancellationToken);

        if (receipt is null)
        {
            return;
        }

        receipt.Detach();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
