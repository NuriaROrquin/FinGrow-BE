namespace FinGrow.Application.Features.Account.ChangePassword;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

/// <summary>
/// HU-04 / HU-49: cambiar la contrasena tiene que cortar cualquier sesion abierta en otro
/// dispositivo. El access token (JWT, T-02) sigue siendo valido hasta que expire porque no
/// consulta la base, pero ya no va a poder renovarse: es lo maximo que se puede lograr sin
/// mover la validacion del JWT a un modelo con estado.
/// </summary>
internal sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private static readonly Error NoAutenticado =
        Error.Unauthorized("Account.NoAutenticado", "Hay que iniciar sesion para cambiar la contrasena.");

    private static readonly Error ContrasenaActualIncorrecta =
        Error.Validation("Account.ContrasenaActualIncorrecta", "La contrasena actual no es correcta.");

    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ChangePasswordCommandHandler(
        ICurrentUser currentUser,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _users = users;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return Result.Failure(NoAutenticado);
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(ContrasenaActualIncorrecta);
        }

        var now = _clock.UtcNow;
        user.ChangePassword(_passwordHasher.Hash(request.NewPassword), now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(userId, now, cancellationToken);

        return Result.Success();
    }
}
