namespace FinGrow.Api.Notifications;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public sealed class NotificationsHub : Hub<INotificationsClient>
{
    public const string Path = "/hubs/notifications";
}
