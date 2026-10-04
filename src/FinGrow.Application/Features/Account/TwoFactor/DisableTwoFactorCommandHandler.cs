namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using MediatR;

internal sealed class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand, Result>
{
    private readonly TwoFactorAccountFinder _accounts;
    private readonly ITotpService _totp;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public DisableTwoFactorCommandHandler(
        TwoFactorAccountFinder accounts,
        ITotpService totp,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _accounts = accounts;
        _totp = totp;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindCurrentAsync(cancellationToken);

        if (account is null)
        {
            return Result.Failure(TwoFactorErrors.NoAutenticado);
        }

        if (!account.IsTwoFactorEnabled)
        {
            return Result.Failure(TwoFactorErrors.NoActivo);
        }

        var now = _clock.UtcNow;

        // Pide el codigo aunque haya sesion: una sesion abierta sola no debe poder quitar la proteccion.
        if (!_totp.VerifyCode(account.TwoFactorSecret!, request.Code, now))
        {
            return Result.Failure(TwoFactorErrors.CodigoInvalido);
        }

        account.DisableTwoFactor(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
