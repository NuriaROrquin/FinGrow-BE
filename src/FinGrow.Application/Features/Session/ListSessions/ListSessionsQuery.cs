namespace FinGrow.Application.Features.Session.ListSessions;

using FinGrow.Application.Common;
using MediatR;

/// <param name="CurrentRefreshToken">El refresh token de la cookie del pedido, para marcar la sesion actual.</param>
public sealed record ListSessionsQuery(string? CurrentRefreshToken) : IRequest<Result<IReadOnlyList<ActiveSessionResponse>>>;
