namespace FinGrow.Application.Features.Integrations.WhatsApp.ReceiveWhatsAppMessage;

using FinGrow.Application.Common;
using MediatR;

public sealed record ReceiveWhatsAppMessageCommand(
    string From,
    string Body,
    string MessageSid,
    IReadOnlyList<WhatsAppInboundMedia> Media) : IRequest<Result<WhatsAppReply>>;

public sealed record WhatsAppInboundMedia(Uri Url, string ContentType);

public sealed record WhatsAppReply(string Text);
