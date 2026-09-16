namespace FinGrow.Application.Features.Integrations.WhatsApp.GetWhatsAppIntegration;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetWhatsAppIntegrationQuery : IRequest<Result<WhatsAppIntegrationResponse>>;

public sealed record WhatsAppIntegrationResponse(bool Linked, string? PhoneNumber, DateTimeOffset? LinkedAt)
{
    public static readonly WhatsAppIntegrationResponse NotLinked = new(false, null, null);
}
