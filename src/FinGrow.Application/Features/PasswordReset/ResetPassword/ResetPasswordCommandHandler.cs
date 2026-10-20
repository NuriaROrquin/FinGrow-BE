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
internal sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<PasswordResetResponse>>
{
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyRepository _companies;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokens,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _tokens = tokens;
        _employees = employees;
        _companies = companies;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PasswordResetResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var token = await _tokens.FindByHashAsync(PasswordResetToken.Hash(request.Token), cancellationToken);

        if (token is null || !token.IsUsable(now))
        {
            return Result.Failure<PasswordResetResponse>(PasswordResetErrors.EnlaceInvalido);
        }

        // Si la empresa le cortó el acceso después de que pidió el enlace, el enlace deja de servir.
        var account = await ResettableAccount.FindByIdAsync(token.Role, token.UserId, _employees, _companies, cancellationToken);

        if (account is null)
        {
            return Result.Failure<PasswordResetResponse>(PasswordResetErrors.EnlaceInvalido);
        }

        token.Redeem(now);
        account.ChangePassword(_passwordHasher.Hash(request.NewPassword), now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _tokens.InvalidateAllForUserAsync(account.Id, now, cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(account.Id, now, cancellationToken);

        return Result.Success(new PasswordResetResponse(account.Role));
    }
}
