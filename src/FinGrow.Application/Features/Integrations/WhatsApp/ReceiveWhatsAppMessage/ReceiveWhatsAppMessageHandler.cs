namespace FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;

using FinGrow.Application.Common;
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
    private readonly IIntegrationLinkCodeRepository _linkCodes;
    private readonly IEmployeeRepository _employees;
    private readonly ITwilioMediaClient _media;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ReceiveWhatsAppMessageHandler> _logger;

    public ReceiveWhatsAppMessageHandler(
        IEmployeeIntegrationRepository integrations,
        IIntegrationLinkCodeRepository linkCodes,
        IEmployeeRepository employees,
        ITwilioMediaClient media,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<ReceiveWhatsAppMessageHandler> logger)
    {
        _integrations = integrations;
        _linkCodes = linkCodes;
        _employees = employees;
        _media = media;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<WhatsAppReply>> Handle(
        ReceiveWhatsAppMessageCommand request,
        CancellationToken cancellationToken)
    {
        var phoneNumber = WhatsAppAddress.ToPhoneNumber(request.From);
        var integration = await _integrations.FindByExternalAccountAsync(
            IntegrationProvider.WhatsApp, phoneNumber, cancellationToken);

        var reply = integration is null
            ? await TryLinkAsync(phoneNumber, request.Body, cancellationToken)
            : await HandleLinkedMessageAsync(integration, request, cancellationToken);

        return Result.Success(reply);
    }

    private async Task<WhatsAppReply> TryLinkAsync(string phoneNumber, string body, CancellationToken cancellationToken)
    {
        var code = IntegrationLinkCode.Normalize(body);

        if (code.Length != IntegrationLinkCode.Length)
        {
            return new WhatsAppReply(NotLinkedReply);
        }

        var now = _clock.UtcNow;
        var linkCode = await _linkCodes.FindByHashAsync(
            IntegrationProvider.WhatsApp, IntegrationLinkCode.Hash(code), cancellationToken);

        if (linkCode is null || !linkCode.IsUsable(now))
        {
            return new WhatsAppReply(InvalidCodeReply);
        }

        var employee = await _employees.GetByIdAsync(linkCode.EmployeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            return new WhatsAppReply(InvalidCodeReply);
        }

        linkCode.Redeem(now);

        var existing = await _integrations.FindByEmployeeAsync(
            employee.Id, IntegrationProvider.WhatsApp, cancellationToken);

        if (existing is null)
        {
            _integrations.Add(EmployeeIntegration.Create(employee.Id, IntegrationProvider.WhatsApp, phoneNumber, now));
        }
        else
        {
            existing.Relink(phoneNumber, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogLinked(_logger, employee.Id);

        return new WhatsAppReply($"Listo, {employee.FullName}{LinkedReplySuffix}");
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

    [LoggerMessage(Level = LogLevel.Information, Message = "WhatsApp vinculado al empleado {EmployeeId}.")]
    private static partial void LogLinked(ILogger logger, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Adjunto de WhatsApp del empleado {EmployeeId}: {ContentType}, {Bytes} bytes (mensaje {MessageSid}).")]
    private static partial void LogAttachment(ILogger logger, Guid employeeId, string contentType, int bytes, string messageSid);

    [LoggerMessage(Level = LogLevel.Information, Message = "Mensaje de WhatsApp del empleado {EmployeeId}: {Characters} caracteres, {Attachments} adjuntos (mensaje {MessageSid}).")]
    private static partial void LogMessage(ILogger logger, Guid employeeId, int characters, int attachments, string messageSid);
}
