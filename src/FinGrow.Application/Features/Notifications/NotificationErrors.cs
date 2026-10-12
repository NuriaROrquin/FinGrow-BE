namespace FinGrow.Application.Features.Notifications;

using FinGrow.Application.Common;

internal static class NotificationErrors
{
    public static Error NotFound(Guid notificationId) =>
        Error.NotFound("Notification.NotFound", $"No existe una notificacion con id '{notificationId}'.");

    public static Error NotOwned() =>
        Error.Forbidden("Notification.NotOwned", "La notificacion no pertenece al usuario autenticado.");
}
