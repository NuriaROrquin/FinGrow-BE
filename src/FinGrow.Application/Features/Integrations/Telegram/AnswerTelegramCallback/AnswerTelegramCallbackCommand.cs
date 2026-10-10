namespace FinGrow.Application.Features.Integrations.Telegram.AnswerTelegramCallback;

using FinGrow.Application.Common;
using MediatR;

public sealed record AnswerTelegramCallbackCommand(long ChatId, string CallbackQueryId, long? MessageId, string Data) : IRequest<Result>;
