namespace FinGrow.Api.Telegram;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Telegram.AnswerTelegramCallback;
using FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;
using MediatR;

internal static class TelegramInboundMessage
{
    public static IRequest<Result>? ToCommand(TelegramUpdate update) =>
        ToMessageCommand(update) ?? (IRequest<Result>?)ToCallbackCommand(update);

    private static ReceiveTelegramMessageCommand? ToMessageCommand(TelegramUpdate update) =>
        update.Message is { Text: { } text, Chat: { IsPrivate: true } chat } message
            ? new ReceiveTelegramMessageCommand(chat.Id, text, message.MessageId)
            : null;

    private static AnswerTelegramCallbackCommand? ToCallbackCommand(TelegramUpdate update)
    {
        if (update.CallbackQuery is not { Data: { } data } callback)
        {
            return null;
        }

        if (callback.Message is { } message)
        {
            return message.Chat.IsPrivate
                ? new AnswerTelegramCallbackCommand(message.Chat.Id, callback.Id, message.MessageId, data)
                : null;
        }

        return new AnswerTelegramCallbackCommand(callback.From.Id, callback.Id, MessageId: null, data);
    }
}
