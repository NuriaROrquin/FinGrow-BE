namespace FinGrow.Application.Features.Notifications.Channels.ChangeNotificationChannel;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ChangeNotificationChannelHandler(
    INotificationChannelSettingRepository channelSettings,
    IEmployeeIntegrationRepository integrations,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IRequestHandler<ChangeNotificationChannelCommand, Result<NotificationChannelResponse>>
{
    public async Task<Result<NotificationChannelResponse>> Handle(
        ChangeNotificationChannelCommand request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var settings = await channelSettings.ListByEmployeeAsync(request.EmployeeId, cancellationToken);
        var setting = settings.FirstOrDefault(existing => existing.Channel == request.Channel);

        if (setting is null)
        {
            setting = NotificationChannelSetting.Create(request.EmployeeId, request.Channel, request.IsEnabled, now);
            channelSettings.Add(setting);
        }
        else
        {
            setting.Change(request.IsEnabled, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(NotificationChannelResponse.From(
            request.Channel,
            setting,
            await integrations.IsConnectedAsync(request.EmployeeId, request.Channel, cancellationToken)));
    }
}
