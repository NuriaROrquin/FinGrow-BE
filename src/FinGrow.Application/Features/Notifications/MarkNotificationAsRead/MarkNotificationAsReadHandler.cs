namespace FinGrow.Application.Features.Notifications.MarkNotificationAsRead;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class MarkNotificationAsReadHandler(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IRequestHandler<MarkNotificationAsReadCommand, Result<NotificationResponse>>
{
    public async Task<Result<NotificationResponse>> Handle(
        MarkNotificationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await notifications.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null || !notification.IsAddressedTo(request.Recipient))
        {
            return Result.Failure<NotificationResponse>(NotificationErrors.NotFound(request.NotificationId));
        }

        if (!notification.IsRead)
        {
            notification.MarkAsRead(clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(NotificationResponse.FromEntity(notification));
    }
}
