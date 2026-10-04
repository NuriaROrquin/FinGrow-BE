namespace FinGrow.Application.Features.Notifications.CountUnreadNotifications;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class CountUnreadNotificationsHandler(INotificationRepository notifications)
    : IRequestHandler<CountUnreadNotificationsQuery, Result<UnreadNotificationsResponse>>
{
    public async Task<Result<UnreadNotificationsResponse>> Handle(
        CountUnreadNotificationsQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(new UnreadNotificationsResponse(
            await notifications.CountAsync(request.Recipient, unreadOnly: true, cancellationToken)));
}
