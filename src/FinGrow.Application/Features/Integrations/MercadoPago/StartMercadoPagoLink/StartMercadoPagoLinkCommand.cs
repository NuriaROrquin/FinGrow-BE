namespace FinGrow.Application.Features.Integrations.MercadoPago.StartMercadoPagoLink;

using FinGrow.Application.Common;
using MediatR;

public sealed record StartMercadoPagoLinkCommand : IRequest<Result<StartMercadoPagoLinkResponse>>;

public sealed record StartMercadoPagoLinkResponse(Uri AuthorizationUrl, DateTimeOffset ExpiresAt);
