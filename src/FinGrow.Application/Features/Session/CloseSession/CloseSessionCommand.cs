namespace FinGrow.Application.Features.Session.CloseSession;

using FinGrow.Application.Common;
using MediatR;

/// <param name="CurrentRefreshToken">El refresh token de la cookie del pedido, para no cerrar la sesion actual.</param>
public sealed record CloseSessionCommand(Guid SessionId, string? CurrentRefreshToken) : IRequest<Result>;
