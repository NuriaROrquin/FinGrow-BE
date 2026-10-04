namespace FinGrow.Application.Features.Notifications.Channels;

using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;

internal static class NotificationChannelConnections
{
    public static async Task<bool> IsConnectedAsync(
        this IEmployeeIntegrationRepository integrations,
        Guid employeeId,
        NotificationChannel channel,
        CancellationToken cancellationToken) =>
        channel switch
        {
            NotificationChannel.InApp => true,
            NotificationChannel.Telegram => await integrations.FindByEmployeeAsync(
                employeeId, IntegrationProvider.Telegram, cancellationToken) is not null,
            _ => false,
        };
}
