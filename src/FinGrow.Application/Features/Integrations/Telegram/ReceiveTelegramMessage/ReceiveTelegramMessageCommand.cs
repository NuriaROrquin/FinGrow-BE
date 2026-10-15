namespace FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;

using FinGrow.Application.Common;
using MediatR;

public sealed record ReceiveTelegramMessageCommand(
    long ChatId,
    string Text,
    long MessageId,
    TelegramVoiceNote? VoiceNote = null,
    TelegramReceiptFile? Receipt = null) : IRequest<Result>;

public sealed record TelegramVoiceNote(string FileId, string ContentType);

public sealed record TelegramReceiptFile(string FileId, string ContentType, string? FileName = null);
