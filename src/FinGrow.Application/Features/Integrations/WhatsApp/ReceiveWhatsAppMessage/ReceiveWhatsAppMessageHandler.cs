namespace FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;

using System.Globalization;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Integrations.ChatTransactions;
using FinGrow.Application.Features.Integrations.Linking;
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

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly LinkCodeRedeemer _linker;
    private readonly ChatTransactionProposer _proposer;
    private readonly WhatsAppProposalReviewer _reviewer;
    private readonly ILogger<ReceiveWhatsAppMessageHandler> _logger;

    public ReceiveWhatsAppMessageHandler(
        IEmployeeIntegrationRepository integrations,
        LinkCodeRedeemer linker,
        ChatTransactionProposer proposer,
        WhatsAppProposalReviewer reviewer,
        ILogger<ReceiveWhatsAppMessageHandler> logger)
    {
        _integrations = integrations;
        _linker = linker;
        _proposer = proposer;
        _reviewer = reviewer;
        _logger = logger;
    }

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
            LinkOutcome.InvalidCode => new WhatsAppReply(InvalidCodeReply),
            _ => new WhatsAppReply($"Listo, {attempt.Employee!.FullName}{LinkedReplySuffix}")
        };
    }

    private async Task<WhatsAppReply> HandleLinkedMessageAsync(
        EmployeeIntegration integration,
        ReceiveWhatsAppMessageCommand request,
        CancellationToken cancellationToken)
    {
        LogMessage(_logger, integration.EmployeeId, request.Body.Length, request.Media.Count, request.MessageSid);

        if (await _reviewer.TryReviewAsync(integration.EmployeeId, request.Body, cancellationToken) is { } review)
        {
            return new WhatsAppReply(ReviewReply(review));
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return new WhatsAppReply(WhatsAppTransactionText.HelpReply);
        }

        return await ProposeAsync(integration.EmployeeId, request, request.Body, heard: null, cancellationToken);
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
            ProposalOutcome.Proposed => WhatsAppTransactionText.Proposal(proposal.Transaction!, proposal.AsksPaymentMethod),
            ProposalOutcome.TooLong => ChatTransactionText.TooLongReply,
            ProposalOutcome.MissingAmount => ChatTransactionText.MissingAmountReply,
            ProposalOutcome.MultipleTransactions => ChatTransactionText.MultipleTransactionsReply,
            ProposalOutcome.NotATransaction => WhatsAppTransactionText.HelpReply,
            _ => ChatTransactionText.AiUnavailableReply
        };

        return new WhatsAppReply(heard is null ? reply : heard + "\n\n" + reply);
    }

    private static string ReviewReply(Review review) =>
        review.Outcome switch
        {
            ReviewOutcome.Confirmed => ChatTransactionText.Confirmed(review.Transaction!),
            ReviewOutcome.Discarded => ChatTransactionText.DiscardedReply,
            _ => ChatTransactionText.AlreadyReviewedReply
        };

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensaje de WhatsApp del empleado {EmployeeId}: {Characters} caracteres, {Attachments} adjuntos (mensaje {MessageSid}).")]
    private static partial void LogMessage(ILogger logger, Guid employeeId, int characters, int attachments, string messageSid);
}
