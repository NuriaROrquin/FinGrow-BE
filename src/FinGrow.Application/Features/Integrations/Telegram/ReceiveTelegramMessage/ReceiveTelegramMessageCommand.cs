namespace FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;

using FinGrow.Application.Common;
using MediatR;

public sealed record ReceiveTelegramMessageCommand(long ChatId, string Text, long MessageId) : IRequest<Result>;
