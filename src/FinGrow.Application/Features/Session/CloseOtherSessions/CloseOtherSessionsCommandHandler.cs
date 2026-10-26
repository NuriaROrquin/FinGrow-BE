namespace FinGrow.Application.Features.Session.CloseOtherSessions;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

/// <summary>
/// Revoca todas las sesiones del usuario menos la del access token del pedido (HU-73). Un token
/// emitido antes de que el access token llevara su sesion no la dice: no revoca nada, porque
/// cerrar todas dejaria afuera tambien a quien lo pidio.
/// </summary>
internal sealed class CloseOtherSessionsCommandHandler : IRequestHandler<CloseOtherSessionsCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IDateTimeProvider _clock;

    public CloseOtherSessionsCommandHandler(ICurrentUser currentUser, IRefreshTokenRepository refreshTokens, IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _refreshTokens = refreshTokens;
        _clock = clock;
    }

    public async Task<Result> Handle(CloseOtherSessionsCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(SessionErrors.Unauthenticated);
        }

        if (_currentUser.SessionId is not { } currentSessionId)
        {
            return Result.Failure(SessionErrors.CurrentUnknown);
        }

        // ExecuteUpdate va directo a la base: no hace falta SaveChanges.
        await _refreshTokens.RevokeAllForUserExceptSessionAsync(userId, currentSessionId, _clock.UtcNow, cancellationToken);

        return Result.Success();
    }
}
