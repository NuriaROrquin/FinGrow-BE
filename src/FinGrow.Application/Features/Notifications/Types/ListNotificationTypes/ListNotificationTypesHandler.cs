namespace FinGrow.Application.Features.Notifications.Types.ListNotificationTypes;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Notifications.Types;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListNotificationTypesHandler(INotificationTypeSettingRepository typeSettings)
    : IRequestHandler<ListNotificationTypesQuery, Result<IReadOnlyList<NotificationTypeResponse>>>
{
    public async Task<Result<IReadOnlyList<NotificationTypeResponse>>> Handle(
        ListNotificationTypesQuery request,
        CancellationToken cancellationToken)
    {
        var settings = await typeSettings.ListByEmployeeAsync(request.EmployeeId, cancellationToken);

        return Result.Success<IReadOnlyList<NotificationTypeResponse>>(Enum.GetValues<NotificationType>()
            .Where(type => !CompanyNotificationTypes.All.Contains(type))
            .Select(type => NotificationTypeResponse.From(type, settings.FirstOrDefault(setting => setting.Type == type)))
            .ToList());
    }
}
