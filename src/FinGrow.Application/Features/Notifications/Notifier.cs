namespace FinGrow.Application.Features.Notifications;

using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal sealed partial class Notifier
{
    private readonly INotificationRepository _notifications;
    private readonly INotificationChannelSettingRepository _channelSettings;
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<Notifier> _logger;

    public Notifier(
        INotificationRepository notifications,
        INotificationChannelSettingRepository channelSettings,
        IEnumerable<INotificationSender> senders,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<Notifier> logger)
    {
        _notifications = notifications;
        _channelSettings = channelSettings;
        _senders = senders;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Notification?> NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.DeduplicationWindow, TimeSpan.Zero);

        var now = _clock.UtcNow;
        var notification = Notification.Create(
            request.Recipient, request.Type, request.Title, request.Body, request.DeduplicationKey, now);

        if (await _notifications.ExistsSinceAsync(
                notification.Recipient,
                notification.Type,
                notification.DeduplicationKey,
                now.Subtract(request.DeduplicationWindow),
                cancellationToken))
        {
            LogDeduplicated(_logger, notification.Type, notification.DeduplicationKey, notification.RecipientId);

            return null;
        }

        _notifications.Add(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await DeliverAsync(notification, cancellationToken);

        return notification;
    }

    private async Task DeliverAsync(Notification notification, CancellationToken cancellationToken)
    {
        var disabled = await DisabledChannelsAsync(notification.Recipient, cancellationToken);

        foreach (var sender in _senders.Where(sender => !disabled.Contains(sender.Channel)))
        {
            try
            {
                if (await sender.SendAsync(notification, cancellationToken))
                {
                    LogDelivered(_logger, notification.Id, sender.Channel);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogDeliveryFailed(_logger, exception, notification.Id, sender.Channel);
            }
        }
    }

    private async Task<IReadOnlySet<NotificationChannel>> DisabledChannelsAsync(
        NotificationRecipient recipient,
        CancellationToken cancellationToken)
    {
        if (recipient.Type != NotificationRecipientType.Employee)
        {
            return new HashSet<NotificationChannel>();
        }

        var settings = await _channelSettings.ListByEmployeeAsync(recipient.Id, cancellationToken);

        return settings
            .Where(setting => !setting.IsEnabled)
            .Select(setting => setting.Channel)
            .ToHashSet();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Alerta {Type} omitida para {RecipientId}: ya se aviso '{DeduplicationKey}' dentro de la ventana de deduplicacion.")]
    private static partial void LogDeduplicated(ILogger logger, NotificationType type, string deduplicationKey, Guid recipientId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Notificacion {NotificationId} entregada por {Channel}.")]
    private static partial void LogDelivered(ILogger logger, Guid notificationId, NotificationChannel channel);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo entregar la notificacion {NotificationId} por {Channel}; queda igual en la bandeja de la app.")]
    private static partial void LogDeliveryFailed(ILogger logger, Exception exception, Guid notificationId, NotificationChannel channel);
}
