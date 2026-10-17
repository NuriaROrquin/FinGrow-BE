namespace FinGrow.Application.Features.Notifications.Types.ChangeNotificationType;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ChangeNotificationTypeHandler(
    INotificationTypeSettingRepository typeSettings,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IRequestHandler<ChangeNotificationTypeCommand, Result<NotificationTypeResponse>>
{
    public async Task<Result<NotificationTypeResponse>> Handle(
        ChangeNotificationTypeCommand request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var settings = await typeSettings.ListByEmployeeAsync(request.EmployeeId, cancellationToken);
        var setting = settings.FirstOrDefault(existing => existing.Type == request.Type);

        if (setting is null)
        {
            setting = NotificationTypeSetting.Create(request.EmployeeId, request.Type, request.IsEnabled, now);
            typeSettings.Add(setting);
        }
        else
        {
            setting.Change(request.IsEnabled, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(NotificationTypeResponse.From(request.Type, setting));
    }
}
