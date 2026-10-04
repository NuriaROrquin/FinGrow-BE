namespace FinGrow.Application.Features.Notifications.Delivery;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;

internal sealed class TelegramNotificationSender : INotificationSender
{
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly ITelegramBotClient _bot;

    public TelegramNotificationSender(IEmployeeIntegrationRepository integrations, ITelegramBotClient bot)
    {
        _integrations = integrations;
        _bot = bot;
    }

    public NotificationChannel Channel => NotificationChannel.Telegram;

    public async Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification.RecipientType != NotificationRecipientType.Employee)
        {
            return false;
        }

        var integration = await _integrations.FindByEmployeeAsync(
            notification.RecipientId, IntegrationProvider.Telegram, cancellationToken);

        if (integration is null
            || !long.TryParse(integration.ExternalAccountId, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var chatId))
        {
            return false;
        }

        await _bot.SendMessageAsync(chatId, string.Concat(notification.Title, "\n\n", notification.Body), cancellationToken);

        return true;
    }
}
