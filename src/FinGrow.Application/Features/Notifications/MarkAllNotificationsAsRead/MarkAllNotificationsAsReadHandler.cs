namespace FinGrow.Application.Features.Notifications.MarkAllNotificationsAsRead;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class MarkAllNotificationsAsReadHandler(
    INotificationRepository notifications,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : IRequestHandler<MarkAllNotificationsAsReadCommand, Result<MarkedNotificationsResponse>>
{
    public async Task<Result<MarkedNotificationsResponse>> Handle(
        MarkAllNotificationsAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var unread = await notifications.ListUnreadAsync(request.Recipient, cancellationToken);

        if (unread.Count == 0)
        {
            return Result.Success(new MarkedNotificationsResponse(0));
        }

        var now = clock.UtcNow;

        foreach (var notification in unread)
        {
            notification.MarkAsRead(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new MarkedNotificationsResponse(unread.Count));
    }
}
