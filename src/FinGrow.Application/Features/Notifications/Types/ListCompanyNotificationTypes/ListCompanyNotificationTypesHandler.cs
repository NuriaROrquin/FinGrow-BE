namespace FinGrow.Application.Features.Notifications.Types.ListCompanyNotificationTypes;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListCompanyNotificationTypesHandler(ICompanyNotificationSettingRepository settings)
    : IRequestHandler<ListCompanyNotificationTypesQuery, Result<IReadOnlyList<NotificationTypeResponse>>>
{
    public async Task<Result<IReadOnlyList<NotificationTypeResponse>>> Handle(
        ListCompanyNotificationTypesQuery request,
        CancellationToken cancellationToken)
    {
        var companySettings = await settings.ListByCompanyAsync(request.CompanyId, cancellationToken);
        return Result.Success<IReadOnlyList<NotificationTypeResponse>>(CompanyNotificationTypes.All
            .Select(type =>
            {
                var setting = companySettings.FirstOrDefault(candidate => candidate.Type == type);
                return new NotificationTypeResponse(
                    type,
                    setting?.IsEnabled ?? false,
                    setting?.Cadence ?? NotificationCadence.Monthly,
                    setting?.Threshold ?? (type == NotificationType.FinancialWellbeingLow ? 60m : type == NotificationType.ParticipationLow ? 50m : null));
            })
            .ToList());
    }
}
