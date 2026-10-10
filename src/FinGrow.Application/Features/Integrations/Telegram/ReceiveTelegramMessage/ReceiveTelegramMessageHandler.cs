namespace FinGrow.Application.Features.Integrations.Telegram.ReceiveTelegramMessage;

using System.Globalization;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class ReceiveTelegramMessageHandler : IRequestHandler<ReceiveTelegramMessageCommand, Result>
{
    internal const string StartCommand = "/start";

    internal const string NotLinkedReply =
        "Este chat todavía no está vinculado a ninguna cuenta de FinGrow. " +
        "Generá un código desde la app (Integraciones → Telegram) y envialo por acá.";

    internal const string InvalidCodeReply =
        "Ese código no es válido o ya venció. Generá uno nuevo desde la app y volvé a intentar.";

    internal const string LinkedReplySuffix = ": este Telegram quedó vinculado a tu cuenta de FinGrow.";

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly LinkCodeRedeemer _linker;
    private readonly ChatTransactionProposer _proposer;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<ReceiveTelegramMessageHandler> _logger;

    public ReceiveTelegramMessageHandler(
        IEmployeeIntegrationRepository integrations,
        LinkCodeRedeemer linker,
        ChatTransactionProposer proposer,
        ITelegramBotClient bot,
        ILogger<ReceiveTelegramMessageHandler> logger)
    {
        _integrations = integrations;
        _linker = linker;
        _proposer = proposer;
        _bot = bot;
        _logger = logger;
    }

    public async Task<Result> Handle(ReceiveTelegramMessageCommand request, CancellationToken cancellationToken)
    {
        var chatId = request.ChatId.ToString(CultureInfo.InvariantCulture);
        var integration = await _integrations.FindByExternalAccountAsync(
            IntegrationProvider.Telegram, chatId, cancellationToken);

        if (integration is null)
        {
            await ReplyAsync(request.ChatId, await TryLinkAsync(chatId, request.Text, cancellationToken), cancellationToken);
        }
        else
        {
            await HandleLinkedMessageAsync(integration.EmployeeId, request, cancellationToken);
        }

        return Result.Success();
    }

    internal static string StripStartCommand(string text)
    {
        var trimmed = text.Trim();

        return trimmed.StartsWith(StartCommand, StringComparison.OrdinalIgnoreCase)
            ? trimmed[StartCommand.Length..]
            : trimmed;
    }

    internal static string ExternalReference(long chatId, long messageId) =>
        string.Create(CultureInfo.InvariantCulture, $"telegram:{chatId}:{messageId}");

    private async Task<string> TryLinkAsync(string chatId, string text, CancellationToken cancellationToken)
    {
        var attempt = await _linker.TryLinkAsync(
            IntegrationProvider.Telegram, chatId, StripStartCommand(text), cancellationToken);

        return attempt.Outcome switch
        {
            LinkOutcome.NotACode => NotLinkedReply,
            LinkOutcome.InvalidCode => InvalidCodeReply,
            _ => $"Listo, {attempt.Employee!.FullName}{LinkedReplySuffix}"
        };
    }

    private async Task HandleLinkedMessageAsync(
        Guid employeeId,
        ReceiveTelegramMessageCommand request,
        CancellationToken cancellationToken)
    {
        LogMessage(_logger, employeeId, request.Text.Length, request.MessageId);

        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.TrimStart().StartsWith('/'))
        {
            await ReplyAsync(request.ChatId, ChatTransactionText.HelpReply, cancellationToken);
            return;
        }

        var proposal = await _proposer.ProposeAsync(
            employeeId,
            TransactionSource.Telegram,
            ExternalReference(request.ChatId, request.MessageId),
            request.Text,
            cancellationToken);

        switch (proposal.Outcome)
        {
            case ProposalOutcome.Proposed:
                await ReplyAsync(
                    request.ChatId,
                    ChatTransactionText.Proposal(proposal.Transaction!, proposal.AsksPaymentMethod),
                    TelegramTransactionCallback.KeyboardFor(proposal.Transaction!, proposal.AsksPaymentMethod),
                    cancellationToken);
                break;
            case ProposalOutcome.AlreadyProposed:
                break;
            case ProposalOutcome.MissingAmount:
                await ReplyAsync(request.ChatId, ChatTransactionText.MissingAmountReply, cancellationToken);
                break;
            case ProposalOutcome.MultipleTransactions:
                await ReplyAsync(request.ChatId, ChatTransactionText.MultipleTransactionsReply, cancellationToken);
                break;
            case ProposalOutcome.NotATransaction:
                await ReplyAsync(request.ChatId, ChatTransactionText.HelpReply, cancellationToken);
                break;
            default:
                await ReplyAsync(request.ChatId, ChatTransactionText.AiUnavailableReply, cancellationToken);
                break;
        }
    }

    private Task ReplyAsync(long chatId, string text, CancellationToken cancellationToken) =>
        SendSafelyAsync(chatId, () => _bot.SendMessageAsync(chatId, text, cancellationToken), cancellationToken);

    private Task ReplyAsync(
        long chatId,
        string text,
        IReadOnlyList<IReadOnlyList<TelegramButton>> buttonRows,
        CancellationToken cancellationToken) =>
        SendSafelyAsync(chatId, () => _bot.SendMessageAsync(chatId, text, buttonRows, cancellationToken), cancellationToken);

    private async Task SendSafelyAsync(long chatId, Func<Task> send, CancellationToken cancellationToken)
    {
        try
        {
            await send();
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensaje de Telegram del empleado {EmployeeId}: {Characters} caracteres (mensaje {MessageId}).")]
    private static partial void LogMessage(ILogger logger, Guid employeeId, int characters, long messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo responder al chat de Telegram {ChatId}.")]
    private static partial void LogReplyFailed(ILogger logger, Exception exception, long chatId);
}
