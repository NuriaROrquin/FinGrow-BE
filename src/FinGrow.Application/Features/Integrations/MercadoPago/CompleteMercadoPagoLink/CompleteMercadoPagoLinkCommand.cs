namespace FinGrow.Application.Features.Integrations.MercadoPago.CompleteMercadoPagoLink;

using FinGrow.Application.Common;
using MediatR;

public sealed record CompleteMercadoPagoLinkCommand(string? Code, string? State, string? Error) : IRequest<Result>;
