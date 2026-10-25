namespace FinGrow.Application.Features.Session;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, Result<LoginResponse>>
{
    private static readonly Error SesionInvalida =
        Error.Unauthorized("Auth.SesionInvalida", "La sesion no es valida, esta vencida o fue cerrada.");

    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly SessionAccountResolver _accounts;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public RefreshSessionCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        SessionAccountResolver accounts,
        SessionIssuer sessionIssuer,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _accounts = accounts;
        _sessionIssuer = sessionIssuer;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Failure<LoginResponse>(SesionInvalida);
        }

        var now = _clock.UtcNow;
        var current = await _refreshTokens.FindByHashAsync(RefreshToken.Hash(request.RefreshToken), cancellationToken);

        if (current is null || !current.IsActive(now))
        {
            return Result.Failure<LoginResponse>(SesionInvalida);
        }

        var user = await _users.GetByIdAsync(current.UserId, cancellationToken);
        var account = user is { IsActive: true }
            ? await _accounts.ResolveAsync(user, cancellationToken)
            : null;

        if (account is null)
        {
            // La cuenta fue dada de baja o borrada despues de haber emitido este refresh token:
            // lo revocamos aunque todavia no haya vencido para no dejar una sesion huerfana.
            current.Revoke(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<LoginResponse>(SesionInvalida);
        }

        var session = _sessionIssuer.Renew(current, account.FullName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }
}
