namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record NotificationTypeResponse(
    NotificationType Type,
    bool IsEnabled,
    NotificationCadence? Cadence = null,
    decimal? Threshold = null)
{
    public static NotificationTypeResponse From(NotificationType type, NotificationTypeSetting? setting) =>
        new(type, setting?.IsEnabled ?? true);
}
