namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using MediatR;

internal sealed class SetupTwoFactorCommandHandler : IRequestHandler<SetupTwoFactorCommand, Result<TwoFactorSetupResponse>>
{
    private readonly TwoFactorAccountFinder _accounts;
    private readonly ITotpService _totp;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public SetupTwoFactorCommandHandler(
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

    public async Task<Result<TwoFactorSetupResponse>> Handle(SetupTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindCurrentAsync(cancellationToken);

        if (account is null)
        {
            return Result.Failure<TwoFactorSetupResponse>(TwoFactorErrors.NoAutenticado);
        }

        if (account.IsTwoFactorEnabled)
        {
            return Result.Failure<TwoFactorSetupResponse>(TwoFactorErrors.YaActivo);
        }

        var secret = _totp.GenerateSecret();
        account.StartTwoFactorEnrollment(secret, _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new TwoFactorSetupResponse(secret, _totp.BuildProvisioningUri(secret, account.Email.Value)));
    }
}
