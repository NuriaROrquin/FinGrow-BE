namespace FinGrow.Application.Features.Notifications;

using FinGrow.Application.Common;

internal static class NotificationErrors
{
    public static Error NotFound(Guid notificationId) =>
        Error.NotFound("Notification.NotFound", $"No existe una notificacion con id '{notificationId}'.");
}
