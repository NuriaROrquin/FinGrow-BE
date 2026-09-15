namespace FinGrow.Application.Features.Integrations.WhatsApp.GenerateWhatsAppLinkCode;

using FinGrow.Application.Common;
using MediatR;

public sealed record GenerateWhatsAppLinkCodeCommand : IRequest<Result<WhatsAppLinkCodeResponse>>;

public sealed record WhatsAppLinkCodeResponse(string Code, DateTimeOffset ExpiresAt);
