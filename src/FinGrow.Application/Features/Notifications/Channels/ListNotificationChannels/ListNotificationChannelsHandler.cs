namespace FinGrow.Application.Features.Notifications.Channels.ListNotificationChannels;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListNotificationChannelsHandler(
    INotificationChannelSettingRepository channelSettings,
    IEmployeeIntegrationRepository integrations)
    : IRequestHandler<ListNotificationChannelsQuery, Result<IReadOnlyList<NotificationChannelResponse>>>
{
    public async Task<Result<IReadOnlyList<NotificationChannelResponse>>> Handle(
        ListNotificationChannelsQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await channelSettings.ListByEmployeeAsync(request.EmployeeId, cancellationToken);
        var channels = new List<NotificationChannelResponse>();

        foreach (var channel in Enum.GetValues<NotificationChannel>())
        {
            channels.Add(NotificationChannelResponse.From(
                channel,
                settings.FirstOrDefault(setting => setting.Channel == channel),
                await integrations.IsConnectedAsync(request.EmployeeId, channel, cancellationToken)));
        }

        return Result.Success<IReadOnlyList<NotificationChannelResponse>>(channels);
    }
}
