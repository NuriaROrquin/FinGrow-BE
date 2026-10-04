namespace FinGrow.Application.Features.Notifications.ListNotifications;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListNotificationsHandler(INotificationRepository notifications)
    : IRequestHandler<ListNotificationsQuery, Result<PagedResult<NotificationResponse>>>
{
    public async Task<Result<PagedResult<NotificationResponse>>> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var total = await notifications.CountAsync(request.Recipient, request.UnreadOnly, cancellationToken);
        var page = await notifications.ListAsync(
            request.Recipient,
            request.UnreadOnly,
            (request.PageNumber - 1) * request.PageSize,
            request.PageSize,
            cancellationToken);

        return Result.Success(new PagedResult<NotificationResponse>(
            page.Select(NotificationResponse.FromEntity).ToList(),
            request.PageNumber,
            request.PageSize,
            total));
    }
}
