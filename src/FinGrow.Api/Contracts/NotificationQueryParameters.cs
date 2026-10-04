namespace FinGrow.Api.Contracts;

using FinGrow.Application.Features.Notifications.ListNotifications;
using Microsoft.AspNetCore.Mvc;

public sealed class NotificationQueryParameters
{
    [FromQuery]
    public bool UnreadOnly { get; init; }

    [FromQuery]
    public int PageNumber { get; init; } = 1;

    [FromQuery]
    public int PageSize { get; init; } = ListNotificationsQuery.DefaultPageSize;
}
