namespace FinGrow.Api.Telegram;

using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.Telegram.AnswerTelegramCallback;
using FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;
using MediatR;

internal static class TelegramInboundMessage
{
    private const string VoiceNoteContentType = "audio/ogg";
    private const string PhotoContentType = "image/jpeg";
    private const string UnknownContentType = "application/octet-stream";

    public static IRequest<Result>? ToCommand(TelegramUpdate update) =>
        ToMessageCommand(update) ?? (IRequest<Result>?)ToCallbackCommand(update);

    private static ReceiveTelegramMessageCommand? ToMessageCommand(TelegramUpdate update) =>
        update.Message switch
        {
            { Chat.IsPrivate: true, Voice: { } voice } message => new ReceiveTelegramMessageCommand(
                message.Chat.Id,
                message.Text ?? string.Empty,
                message.MessageId,
                new TelegramVoiceNote(voice.FileId, voice.MimeType ?? VoiceNoteContentType)),
            { Chat.IsPrivate: true, Photo: { Count: > 0 } photos } message => new ReceiveTelegramMessageCommand(
                message.Chat.Id,
                string.Empty,
                message.MessageId,
                Receipt: new TelegramReceiptFile(photos[photos.Count - 1].FileId, PhotoContentType)),
            { Chat.IsPrivate: true, Document: { } document } message => new ReceiveTelegramMessageCommand(
                message.Chat.Id,
                string.Empty,
                message.MessageId,
                Receipt: new TelegramReceiptFile(document.FileId, document.MimeType ?? UnknownContentType, document.FileName)),
            { Chat.IsPrivate: true, Text: { } text } message => new ReceiveTelegramMessageCommand(
                message.Chat.Id, text, message.MessageId),
            _ => null
        };

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
