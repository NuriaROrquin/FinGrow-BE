namespace FinGrow.Application.Features.Notifications.Alerts.IntegrationAccessLost;

using FinGrow.Application.Events;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class NotifyIntegrationAccessLostHandler(Notifier notifier)
    : INotificationHandler<DomainEventEnvelope<IntegrationAccessLost>>
{
    internal static readonly TimeSpan DeduplicationWindow = TimeSpan.FromDays(1);

    public async Task Handle(DomainEventEnvelope<IntegrationAccessLost> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var lost = notification.DomainEvent;
        var name = DisplayName(lost.Provider);

        await notifier.NotifyAsync(
            new NotificationRequest(
                NotificationRecipient.Employee(lost.EmployeeId),
                NotificationType.IntegrationAccessLost,
                $"Perdimos el acceso a {name}",
                $"No pudimos sincronizar {name}, probablemente porque quitaste el permiso. Volve a autorizarlo desde Configuracion para seguir registrando tus movimientos.",
                $"integration-access-lost:{lost.Provider}",
                DeduplicationWindow),
            cancellationToken);
    }

    private static string DisplayName(IntegrationProvider provider) =>
        provider switch
        {
            IntegrationProvider.MercadoPago => "Mercado Pago",
            IntegrationProvider.Iol => "InvertirOnline",
            _ => provider.ToString(),
        };
}
