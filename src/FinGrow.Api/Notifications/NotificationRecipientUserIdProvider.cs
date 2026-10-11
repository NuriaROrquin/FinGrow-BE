namespace FinGrow.Api.Notifications;

using System.Security.Claims;
using FinGrow.Application.Common;
using FinGrow.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;

internal sealed class NotificationRecipientUserIdProvider : IUserIdProvider
{
    public static string UserIdOf(NotificationRecipient recipient)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        return $"{recipient.Type}:{recipient.Id}";
    }

    public string? GetUserId(HubConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        var user = connection.User;

        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || userId == Guid.Empty)
        {
            return null;
        }

        var recipient = user.FindFirstValue(ClaimTypes.Role) == Rol.Empresa
            ? NotificationRecipient.Company(userId)
            : NotificationRecipient.Employee(userId);

        return UserIdOf(recipient);
    }
}
