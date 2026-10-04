namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record NotificationResponse(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt)
{
    public static NotificationResponse FromEntity(Notification notification) => new(
        notification.Id,
        notification.Type,
        notification.Title,
        notification.Body,
        notification.IsRead,
        notification.CreatedAt,
        notification.ReadAt);
}
