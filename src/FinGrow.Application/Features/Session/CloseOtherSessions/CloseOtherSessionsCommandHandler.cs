namespace FinGrow.Application.Features.Session.CloseOtherSessions;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

/// <summary>
/// Revoca todas las sesiones del usuario menos la del pedido (HU-73). Si no se puede saber cual
/// es la actual no revoca nada: cerrar todas dejaria afuera tambien a quien lo pidio.
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

        var now = _clock.UtcNow;
        var current = await CurrentSession.FindAsync(_refreshTokens, request.CurrentRefreshToken, userId, now, cancellationToken);

        if (current is null)
        {
            return Result.Failure(SessionErrors.CurrentUnknown);
        }

        // ExecuteUpdate va directo a la base: no hace falta SaveChanges.
        await _refreshTokens.RevokeAllForUserExceptSessionAsync(userId, current.SessionId, now, cancellationToken);

        return Result.Success();
    }
}
