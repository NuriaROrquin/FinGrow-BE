namespace FinGrow.Application.Features.Notifications.Alerts.TransactionsToReview;

using FinGrow.Application.Events;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class NotifyTransactionsToReviewHandler(Notifier notifier)
    : INotificationHandler<DomainEventEnvelope<TransactionProposed>>
{
    internal const string Title = "Movimientos para revisar";

    internal static readonly TimeSpan DeduplicationWindow = TimeSpan.FromHours(12);

    public async Task Handle(DomainEventEnvelope<TransactionProposed> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var proposed = notification.DomainEvent;
        var origin = UnattendedOrigin(proposed.Source);

        if (origin is null)
        {
            return;
        }

        await notifier.NotifyAsync(
            new NotificationRequest(
                NotificationRecipient.Employee(proposed.EmployeeId),
                NotificationType.TransactionsToReview,
                Title,
                $"Entraron movimientos nuevos desde {origin}. Revisalos para que cuenten en tu saldo y en tus presupuestos.",
                $"transactions-to-review:{proposed.Source}",
                DeduplicationWindow),
            cancellationToken);
    }

    private static string? UnattendedOrigin(TransactionSource source) =>
        source switch
        {
            TransactionSource.MercadoPago => "Mercado Pago",
            TransactionSource.Gmail => "Gmail",
            _ => null,
        };
}
