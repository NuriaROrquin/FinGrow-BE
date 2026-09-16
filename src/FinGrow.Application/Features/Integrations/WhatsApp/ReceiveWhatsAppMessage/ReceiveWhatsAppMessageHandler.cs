namespace FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;

using FinGrow.Application.Common;
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

    internal const string MessageReceivedReply =
        "Recibido. Todavía no registro movimientos por WhatsApp, pero tu número ya está vinculado.";

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly LinkCodeRedeemer _linker;
    private readonly ITwilioMediaClient _media;
    private readonly ILogger<ReceiveWhatsAppMessageHandler> _logger;

    public ReceiveWhatsAppMessageHandler(
        IEmployeeIntegrationRepository integrations,
        LinkCodeRedeemer linker,
        ITwilioMediaClient media,
        ILogger<ReceiveWhatsAppMessageHandler> logger)
    {
        _integrations = integrations;
        _linker = linker;
        _media = media;
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
        foreach (var attachment in request.Media)
        {
            var media = await _media.DownloadAsync(attachment.Url, cancellationToken);

            LogAttachment(_logger, integration.EmployeeId, media.ContentType, media.Content.Length, request.MessageSid);
        }

        LogMessage(_logger, integration.EmployeeId, request.Body.Length, request.Media.Count, request.MessageSid);

        return new WhatsAppReply(MessageReceivedReply);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Adjunto de WhatsApp del empleado {EmployeeId}: {ContentType}, {Bytes} bytes (mensaje {MessageSid}).")]
    private static partial void LogAttachment(ILogger logger, Guid employeeId, string contentType, int bytes, string messageSid);

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensaje de WhatsApp del empleado {EmployeeId}: {Characters} caracteres, {Attachments} adjuntos (mensaje {MessageSid}).")]
    private static partial void LogMessage(ILogger logger, Guid employeeId, int characters, int attachments, string messageSid);
}
