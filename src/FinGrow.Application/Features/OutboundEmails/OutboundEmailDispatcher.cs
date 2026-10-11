namespace FinGrow.Application.Features.OutboundEmails;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using Microsoft.Extensions.Logging;

internal sealed partial class OutboundEmailDispatcher
{
    private readonly IEmailSender _sender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<OutboundEmailDispatcher> _logger;

    public OutboundEmailDispatcher(
        IEmailSender sender,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<OutboundEmailDispatcher> logger)
    {
        _sender = sender;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task DispatchAsync(OutboundEmail email, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        try
        {
            var message = new EmailMessage(email.Recipient, email.Subject, email.HtmlBody!, email.TextBody!);
            var providerMessageId = await _sender.SendAsync(message, cancellationToken);

            email.MarkSent(providerMessageId, _clock.UtcNow);
        }
        catch (EmailDeliveryException exception)
        {
            email.MarkFailed(exception.Message, _clock.UtcNow);
            LogDeliveryFailed(_logger, exception, email.Id, email.Attempts, email.Status);
        }

        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo enviar el correo {OutboundEmailId} (intento {Attempts}); quedo {Status}.")]
    private static partial void LogDeliveryFailed(
        ILogger logger,
        Exception exception,
        Guid outboundEmailId,
        int attempts,
        OutboundEmailStatus status);
}
