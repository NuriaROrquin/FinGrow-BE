namespace FinGrow.Application.Features.Session.CloseOtherSessions;

using FinGrow.Application.Common;
using MediatR;

/// <param name="CurrentRefreshToken">El refresh token de la cookie del pedido: la sesion que se conserva.</param>
public sealed record CloseOtherSessionsCommand(string? CurrentRefreshToken) : IRequest<Result>;
