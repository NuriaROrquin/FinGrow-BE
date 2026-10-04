namespace FinGrow.Application.Features.Notifications.Delivery;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public interface INotificationSender
{
    NotificationChannel Channel { get; }

    Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
