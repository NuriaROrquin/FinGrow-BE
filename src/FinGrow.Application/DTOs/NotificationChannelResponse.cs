namespace FinGrow.Application.DTOs;

using Domain.Entities;
using Domain.Enums;

public sealed record NotificationChannelResponse(
    NotificationChannel Channel,
    bool IsEnabled,
    bool IsConfigurable,
    bool IsConnected)
{
    public static NotificationChannelResponse From(
        NotificationChannel channel,
        NotificationChannelSetting? setting,
        bool isConnected) => new(
        channel,
        setting?.IsEnabled ?? true,
        channel.IsConfigurable(),
        isConnected);
}
