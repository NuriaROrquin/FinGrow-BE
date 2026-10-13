namespace FinGrow.Application.Features.Notifications.Alerts.AchievementUnlocked;

using FinGrow.Application.Events;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using MediatR;
using AchievementUnlockedEvent = FinGrow.Domain.Events.AchievementUnlocked;

/// <summary>
/// La notificacion es la señal para que el front celebre en el momento: los datos de la medalla los
/// pide a /api/achievements/uncelebrated, que es tambien lo que consulta al abrir la app.
/// </summary>
internal sealed class NotifyAchievementUnlockedHandler(Notifier notifier)
    : INotificationHandler<DomainEventEnvelope<AchievementUnlockedEvent>>
{
    internal const string TitlePrefix = "Logro desbloqueado";

    // Un logro se otorga una sola vez; la ventana solo cubre un evento reprocesado.
    internal static readonly TimeSpan DeduplicationWindow = TimeSpan.FromDays(366 * 10);

    public async Task Handle(DomainEventEnvelope<AchievementUnlockedEvent> notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var unlocked = notification.DomainEvent;
        var achievement = AchievementCatalog.Find(unlocked.Code);

        await notifier.NotifyAsync(
            new NotificationRequest(
                NotificationRecipient.Employee(unlocked.EmployeeId),
                NotificationType.AchievementUnlocked,
                $"{TitlePrefix}: {achievement.Name}",
                achievement.Description,
                $"achievement:{unlocked.Code}",
                DeduplicationWindow),
            cancellationToken);
    }
}
