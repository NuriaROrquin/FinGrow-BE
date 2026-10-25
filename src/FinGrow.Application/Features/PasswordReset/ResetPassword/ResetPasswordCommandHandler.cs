namespace FinGrow.Application.Features.PasswordReset.ResetPassword;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

/// <summary>
/// HU-72: como el cambio de contraseña (HU-04), cierra las sesiones abiertas. No toca el doble
/// factor: quien lo tiene activo lo sigue necesitando para entrar con la contraseña nueva.
/// </summary>
internal sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokens,
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _tokens = tokens;
        _users = users;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var token = await _tokens.FindByHashAsync(PasswordResetToken.Hash(request.Token), cancellationToken);

        if (token is null || !token.IsUsable(now))
        {
            return Result.Failure(PasswordResetErrors.EnlaceInvalido);
        }

        // Si la empresa le cortó el acceso después de que pidió el enlace, el enlace deja de servir.
        var user = await _users.GetByIdAsync(token.UserId, cancellationToken);

        if (user is not { IsActive: true })
        {
            return Result.Failure(PasswordResetErrors.EnlaceInvalido);
        }

        token.Redeem(now);
        user.ChangePassword(_passwordHasher.Hash(request.NewPassword), now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _tokens.InvalidateAllForUserAsync(user.Id, now, cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(user.Id, now, cancellationToken);

        return Result.Success();
    }
}
