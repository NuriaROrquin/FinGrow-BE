namespace FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;

using System.Globalization;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Features.Integrations.Linking;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class ReceiveWhatsAppMessageHandler
    : IRequestHandler<ReceiveWhatsAppMessageCommand, Result<WhatsAppReply>>
{
    internal const string NotLinkedReply =
        "Este número todavía no está vinculado a ninguna cuenta de FinGrow. " +
        "Generá un código desde la app (Integraciones → WhatsApp) y envialo por acá.";

    internal const string InvalidCodeReply =
        "Ese código no es válido o ya venció. Generá uno nuevo desde la app y volvé a intentar.";

    internal const string LinkedReplySuffix = ": este WhatsApp quedó vinculado a tu cuenta de FinGrow.";

    internal static readonly TimeSpan DefaultProcessingBudget = TimeSpan.FromSeconds(12);

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly LinkCodeRedeemer _linker;
    private readonly ITwilioMediaClient _media;
    private readonly ChatVoiceNoteTranscriber _voiceNotes;
    private readonly ChatTransactionProposer _proposer;
    private readonly WhatsAppProposalReviewer _reviewer;
    private readonly ILogger<ReceiveWhatsAppMessageHandler> _logger;

    public ReceiveWhatsAppMessageHandler(
        IEmployeeIntegrationRepository integrations,
        LinkCodeRedeemer linker,
        ITwilioMediaClient media,
        ChatVoiceNoteTranscriber voiceNotes,
        ChatTransactionProposer proposer,
        WhatsAppProposalReviewer reviewer,
        ILogger<ReceiveWhatsAppMessageHandler> logger)
    {
        _integrations = integrations;
        _linker = linker;
        _media = media;
        _voiceNotes = voiceNotes;
        _proposer = proposer;
        _reviewer = reviewer;
        _logger = logger;
    }

    internal TimeSpan ProcessingBudget { get; init; } = DefaultProcessingBudget;

    public async Task<Result<WhatsAppReply>> Handle(
        ReceiveWhatsAppMessageCommand request,
        CancellationToken cancellationToken)
    {
        var integration = await _integrations.FindByExternalAccountAsync(
            IntegrationProvider.WhatsApp, request.PhoneNumber, cancellationToken);

        var reply = integration is null
            ? await TryLinkAsync(request.PhoneNumber, request.Body, cancellationToken)
            : await HandleLinkedMessageAsync(integration, request, cancellationToken);

        return Result.Success(reply);
    }

    internal static string ExternalReference(string phoneNumber, string messageSid) =>
        string.Create(CultureInfo.InvariantCulture, $"whatsapp:{phoneNumber}:{messageSid}");

    private async Task<WhatsAppReply> TryLinkAsync(string phoneNumber, string body, CancellationToken cancellationToken)
    {
        var attempt = await _linker.TryLinkAsync(IntegrationProvider.WhatsApp, phoneNumber, body, cancellationToken);

        return attempt.Outcome switch
        {
            LinkOutcome.NotACode => new WhatsAppReply(NotLinkedReply),
            LinkOutcome.Linked => new WhatsAppReply($"Listo, {attempt.Employee!.FullName}{LinkedReplySuffix}"),
            _ => new WhatsAppReply(InvalidCodeReply)
        };
    }

    private async Task<WhatsAppReply> HandleLinkedMessageAsync(
        EmployeeIntegration integration,
        ReceiveWhatsAppMessageCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = integration.EmployeeId;
        var voiceNote = request.Media.FirstOrDefault(IsVoiceNote);

        LogMessage(_logger, employeeId, request.Body.Length, request.Media.Count, request.MessageSid);

        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(ProcessingBudget);

        try
        {
            return voiceNote is null
                ? await HandleTextAsync(employeeId, request, budget.Token)
                : await HandleVoiceNoteAsync(employeeId, request, voiceNote, budget.Token);
        }
        catch (OperationCanceledException) when (budget.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            LogOutOfTime(_logger, employeeId, request.MessageSid);

            return new WhatsAppReply(ChatTransactionText.AiUnavailableReply);
        }
    }

    private async Task<WhatsAppReply> HandleTextAsync(
        Guid employeeId,
        ReceiveWhatsAppMessageCommand request,
        CancellationToken cancellationToken)
    {
        if (await _reviewer.TryReviewAsync(employeeId, request.Body, cancellationToken) is { } review)
        {
            return new WhatsAppReply(ReviewReply(review));
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return new WhatsAppReply(ChatTransactionText.HelpReply);
        }

        return await ProposeAsync(employeeId, request, request.Body, heard: null, cancellationToken);
    }

    private async Task<WhatsAppReply> HandleVoiceNoteAsync(
        Guid employeeId,
        ReceiveWhatsAppMessageCommand request,
        WhatsAppInboundMedia attachment,
        CancellationToken cancellationToken)
    {
        var voiceNote = await _voiceNotes.TranscribeAsync(
            employeeId,
            async token => (await _media.DownloadAsync(attachment.Url, token)).Content,
            attachment.ContentType,
            cancellationToken);

        if (voiceNote is not { Outcome: VoiceNoteOutcome.Transcribed, Text: { } text })
        {
            return new WhatsAppReply(voiceNote.Outcome == VoiceNoteOutcome.NotUnderstood
                ? ChatTransactionText.AudioNotUnderstoodReply
                : ChatTransactionText.AiUnavailableReply);
        }

        var heard = ChatTransactionText.Heard(text);

        if (await _reviewer.TryReviewAsync(employeeId, text, cancellationToken) is { } review)
        {
            return new WhatsAppReply(heard + "\n\n" + ReviewReply(review));
        }

        return await ProposeAsync(employeeId, request, text, heard, cancellationToken);
    }

    private async Task<WhatsAppReply> ProposeAsync(
        Guid employeeId,
        ReceiveWhatsAppMessageCommand request,
        string text,
        string? heard,
        CancellationToken cancellationToken)
    {
        var proposal = await _proposer.ProposeAsync(
            employeeId,
            TransactionSource.WhatsApp,
            ExternalReference(request.PhoneNumber, request.MessageSid),
            text,
            cancellationToken);

        if (proposal.Outcome == ProposalOutcome.AlreadyProposed)
        {
            return WhatsAppReply.Silent;
        }

        var reply = proposal.Outcome switch
        {
            ProposalOutcome.Proposed => WhatsAppTransactionText.Proposal(proposal.Transaction!, proposal.AsksPaymentMethod, proposal.DateIsEstimate),
            ProposalOutcome.TooLong => ChatTransactionText.TooLongReply,
            ProposalOutcome.MissingAmount => ChatTransactionText.MissingAmountReply,
            ProposalOutcome.MultipleTransactions => ChatTransactionText.MultipleTransactionsReply,
            ProposalOutcome.NotATransaction => ChatTransactionText.HelpReply,
            _ => ChatTransactionText.AiUnavailableReply
        };

        return new WhatsAppReply(heard is null ? reply : heard + "\n\n" + reply);
    }

    private static bool IsVoiceNote(WhatsAppInboundMedia attachment) =>
        attachment.ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);

    private static string ReviewReply(Review review) =>
        review.Outcome switch
        {
            ReviewOutcome.Confirmed => ChatTransactionText.Confirmed(review.Transaction!),
            ReviewOutcome.Discarded => ChatTransactionText.DiscardedReply,
            _ => ChatTransactionText.AlreadyReviewedReply
        };

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensaje de WhatsApp del empleado {EmployeeId}: {Characters} caracteres, {Attachments} adjuntos (mensaje {MessageSid}).")]
    private static partial void LogMessage(ILogger logger, Guid employeeId, int characters, int attachments, string messageSid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "El mensaje de WhatsApp {MessageSid} del empleado {EmployeeId} no se proceso a tiempo para responderle a Twilio.")]
    private static partial void LogOutOfTime(ILogger logger, Guid employeeId, string messageSid);
}
