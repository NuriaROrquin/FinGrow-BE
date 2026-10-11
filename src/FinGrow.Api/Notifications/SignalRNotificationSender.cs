namespace FinGrow.Api.Notifications;

using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.AspNetCore.SignalR;

internal sealed class SignalRNotificationSender(IHubContext<NotificationsHub, INotificationsClient> hub) : INotificationSender
{
    public NotificationChannel Channel => NotificationChannel.InApp;

    public async Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        await hub.Clients
            .User(NotificationRecipientUserIdProvider.UserIdOf(notification.Recipient))
            .NotificationReceived(NotificationResponse.FromEntity(notification));

        return true;
    }
}
