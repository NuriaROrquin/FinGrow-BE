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
    private readonly ChatVoiceNoteTranscriber _voiceNotes;
    private readonly ChatTransactionProposer _proposer;
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<ReceiveTelegramMessageHandler> _logger;

    public ReceiveTelegramMessageHandler(
        IEmployeeIntegrationRepository integrations,
        LinkCodeRedeemer linker,
        ChatVoiceNoteTranscriber voiceNotes,
        ChatTransactionProposer proposer,
        ITelegramBotClient bot,
        ILogger<ReceiveTelegramMessageHandler> logger)
    {
        _integrations = integrations;
        _linker = linker;
        _voiceNotes = voiceNotes;
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
            LinkOutcome.Linked => $"Listo, {attempt.Employee!.FullName}{LinkedReplySuffix}",
            _ => InvalidCodeReply
        };
    }

    private async Task HandleLinkedMessageAsync(
        Guid employeeId,
        ReceiveTelegramMessageCommand request,
        CancellationToken cancellationToken)
    {
        LogMessage(_logger, employeeId, request.Text.Length, request.VoiceNote is not null, request.MessageId);

        if (request.VoiceNote is { } voiceNote)
        {
            await HandleVoiceNoteAsync(employeeId, request, voiceNote, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.TrimStart().StartsWith('/'))
        {
            await ReplyAsync(request.ChatId, ChatTransactionText.HelpReply, cancellationToken);
            return;
        }

        await ProposeAsync(employeeId, request, request.Text, heard: null, cancellationToken);
    }

    private async Task HandleVoiceNoteAsync(
        Guid employeeId,
        ReceiveTelegramMessageCommand request,
        TelegramVoiceNote voiceNote,
        CancellationToken cancellationToken)
    {
        var transcription = await _voiceNotes.TranscribeAsync(
            employeeId,
            token => _bot.DownloadFileAsync(voiceNote.FileId, token),
            voiceNote.ContentType,
            cancellationToken);

        if (transcription is not { Outcome: VoiceNoteOutcome.Transcribed, Text: { } text })
        {
            var reply = transcription.Outcome == VoiceNoteOutcome.NotUnderstood
                ? ChatTransactionText.AudioNotUnderstoodReply
                : ChatTransactionText.AiUnavailableReply;

            await ReplyAsync(request.ChatId, reply, cancellationToken);
            return;
        }

        await ProposeAsync(employeeId, request, text, ChatTransactionText.Heard(text), cancellationToken);
    }

    private async Task ProposeAsync(
        Guid employeeId,
        ReceiveTelegramMessageCommand request,
        string text,
        string? heard,
        CancellationToken cancellationToken)
    {
        var proposal = await _proposer.ProposeAsync(
            employeeId,
            TransactionSource.Telegram,
            ExternalReference(request.ChatId, request.MessageId),
            text,
            cancellationToken);

        switch (proposal.Outcome)
        {
            case ProposalOutcome.Proposed:
                await ReplyAsync(
                    request.ChatId,
                    WithHeard(heard, ChatTransactionText.Proposal(proposal.Transaction!, proposal.AsksPaymentMethod)),
                    TelegramTransactionCallback.KeyboardFor(proposal.Transaction!, proposal.AsksPaymentMethod),
                    cancellationToken);
                break;
            case ProposalOutcome.AlreadyProposed:
                break;
            case ProposalOutcome.TooLong:
                await ReplyAsync(request.ChatId, WithHeard(heard, ChatTransactionText.TooLongReply), cancellationToken);
                break;
            case ProposalOutcome.MissingAmount:
                await ReplyAsync(request.ChatId, WithHeard(heard, ChatTransactionText.MissingAmountReply), cancellationToken);
                break;
            case ProposalOutcome.MultipleTransactions:
                await ReplyAsync(request.ChatId, WithHeard(heard, ChatTransactionText.MultipleTransactionsReply), cancellationToken);
                break;
            case ProposalOutcome.NotATransaction:
                await ReplyAsync(request.ChatId, WithHeard(heard, ChatTransactionText.HelpReply), cancellationToken);
                break;
            default:
                await ReplyAsync(request.ChatId, ChatTransactionText.AiUnavailableReply, cancellationToken);
                break;
        }
    }

    private static string WithHeard(string? heard, string reply) => heard is null ? reply : heard + "\n\n" + reply;

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensaje de Telegram del empleado {EmployeeId}: {Characters} caracteres, nota de voz {IsVoiceNote} (mensaje {MessageId}).")]
    private static partial void LogMessage(ILogger logger, Guid employeeId, int characters, bool isVoiceNote, long messageId);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudo responder al chat de Telegram {ChatId}.")]
    private static partial void LogReplyFailed(ILogger logger, Exception exception, long chatId);
}
