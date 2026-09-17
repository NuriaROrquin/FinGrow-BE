namespace FinGrow.Api.Telegram;

using FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;

internal static class TelegramInboundMessage
{
    public static ReceiveTelegramMessageCommand? ToCommand(TelegramUpdate update) =>
        update.Message is { Text: { } text, Chat: { IsPrivate: true } chat } message
            ? new ReceiveTelegramMessageCommand(chat.Id, text, message.MessageId)
            : null;
}
