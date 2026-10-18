namespace FinGrow.Application.Features.Notifications.Types.ChangeCompanyNotificationType;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ChangeCompanyNotificationTypeHandler(
    ICompanyNotificationSettingRepository settings,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IRequestHandler<ChangeCompanyNotificationTypeCommand, Result<NotificationTypeResponse>>
{
    public async Task<Result<NotificationTypeResponse>> Handle(
        ChangeCompanyNotificationTypeCommand request,
        CancellationToken cancellationToken)
    {
        var existing = (await settings.ListByCompanyAsync(request.CompanyId, cancellationToken))
            .FirstOrDefault(setting => setting.Type == request.Type);
        var now = clock.UtcNow;

        if (existing is null)
        {
            existing = CompanyNotificationSetting.Create(
                request.CompanyId, request.Type, request.IsEnabled, request.Cadence, now, request.Threshold);
            settings.Add(existing);
        }
        else
        {
            existing.Change(request.IsEnabled, request.Cadence, now, request.Threshold);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new NotificationTypeResponse(existing.Type, existing.IsEnabled, existing.Cadence, existing.Threshold));
    }
}
