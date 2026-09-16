namespace FinGrow.Application.Features.Integrations.WhatsApp.UnlinkWhatsApp;

using FinGrow.Application.Common;
using MediatR;

public sealed record UnlinkWhatsAppCommand : IRequest<Result>;
