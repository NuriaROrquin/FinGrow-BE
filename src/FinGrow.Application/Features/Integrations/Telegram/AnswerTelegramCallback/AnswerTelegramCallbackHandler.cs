namespace FinGrow.Application.Features.Integrations.Telegram.AnswerTelegramCallback;

using System.Globalization;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class AnswerTelegramCallbackHandler : IRequestHandler<AnswerTelegramCallbackCommand, Result>
{
    internal const string UnknownButtonReply = "Ese botón ya no está disponible.";

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly ChatTransactionReviewer _reviewer;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<AnswerTelegramCallbackHandler> _logger;

    public AnswerTelegramCallbackHandler(
        IEmployeeIntegrationRepository integrations,
        ChatTransactionReviewer reviewer,
        ITelegramBotClient bot,
        ILogger<AnswerTelegramCallbackHandler> logger)
    {
        _integrations = integrations;
        _reviewer = reviewer;
        _bot = bot;
        _logger = logger;
    }

    public async Task<Result> Handle(AnswerTelegramCallbackCommand request, CancellationToken cancellationToken)
    {
        var integration = await _integrations.FindByExternalAccountAsync(
            IntegrationProvider.Telegram, request.ChatId.ToString(CultureInfo.InvariantCulture), cancellationToken);

        if (integration is null)
        {
            await SafelyAsync(
                request.ChatId,
                () => _bot.AnswerCallbackAsync(request.CallbackQueryId, ReceiveTelegramMessageHandler.NotLinkedReply, cancellationToken),
                cancellationToken);
            return Result.Success();
        }

        if (TelegramTransactionCallback.Parse(request.Data) is not { } callback)
        {
            await SafelyAsync(
                request.ChatId,
                () => _bot.AnswerCallbackAsync(request.CallbackQueryId, UnknownButtonReply, cancellationToken),
                cancellationToken);
            return Result.Success();
        }

        var review = await _reviewer.ReviewAsync(
            integration.EmployeeId,
            callback.TransactionId,
            callback.Decision,
            callback.PaymentMethod,
            cancellationToken);

        var reply = review.Outcome switch
        {
            ReviewOutcome.Confirmed => ChatTransactionText.Confirmed(review.Transaction!),
            ReviewOutcome.Discarded => ChatTransactionText.DiscardedReply,
            _ => ChatTransactionText.AlreadyReviewedReply
        };

        await SafelyAsync(request.ChatId, () => _bot.AnswerCallbackAsync(request.CallbackQueryId, null, cancellationToken), cancellationToken);

        await SafelyAsync(
            request.ChatId,
            () => request.MessageId is { } messageId
                ? _bot.EditMessageAsync(request.ChatId, messageId, reply, cancellationToken)
                : _bot.SendMessageAsync(request.ChatId, reply, cancellationToken),
            cancellationToken);

        return Result.Success();
    }

    private async Task SafelyAsync(long chatId, Func<Task> call, CancellationToken cancellationToken)
    {
        try
        {
            await call();
        }
        catch (HttpRequestException exception)
        {
            LogReplyFailed(_logger, exception, chatId);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogReplyFailed(_logger, exception, chatId);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo responder al boton tocado en el chat de Telegram {ChatId}.")]
    private static partial void LogReplyFailed(ILogger logger, Exception exception, long chatId);
}
