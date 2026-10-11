namespace FinGrow.Api.Notifications;

using FinGrow.Application.DTOs;

public interface INotificationsClient
{
    Task NotificationReceived(NotificationResponse notification);
}
