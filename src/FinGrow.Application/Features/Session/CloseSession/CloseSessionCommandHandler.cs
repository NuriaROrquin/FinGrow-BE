namespace FinGrow.Application.Features.Session.CloseSession;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

/// <summary>
/// Revoca el refresh token vigente de otra sesion del usuario (HU-73). Ese dispositivo sigue
/// entrando hasta que vence su access token y vuelve al login en el proximo refresh, igual que al
/// desactivar un empleado o cambiar la contrasena.
/// </summary>
internal sealed class CloseSessionCommandHandler : IRequestHandler<CloseSessionCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CloseSessionCommandHandler(
        ICurrentUser currentUser,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(CloseSessionCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(SessionErrors.Unauthenticated);
        }

        var now = _clock.UtcNow;
        var token = await _refreshTokens.FindActiveBySessionAsync(request.SessionId, now, cancellationToken);
        var owned = Ownership.Require(token, t => t.UserId, userId, SessionErrors.NotFound, SessionErrors.NotOwned);

        if (owned.IsFailure)
        {
            return owned;
        }

        if (request.SessionId == _currentUser.SessionId)
        {
            return Result.Failure(SessionErrors.IsCurrent);
        }

        owned.Value.Revoke(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
