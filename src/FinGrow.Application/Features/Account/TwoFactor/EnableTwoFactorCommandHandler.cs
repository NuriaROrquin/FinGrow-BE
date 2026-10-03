namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class EnableTwoFactorCommandHandler : IRequestHandler<EnableTwoFactorCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly ITotpService _totp;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public EnableTwoFactorCommandHandler(
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        ITotpService totp,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _employees = employees;
        _totp = totp;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure(TwoFactorErrors.NoAutenticado);
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure(TwoFactorErrors.NoAutenticado);
        }

        if (employee.IsTwoFactorEnabled)
        {
            return Result.Failure(TwoFactorErrors.YaActivo);
        }

        if (employee.TwoFactorSecret is not { } secret)
        {
            return Result.Failure(TwoFactorErrors.AltaNoIniciada);
        }

        var now = _clock.UtcNow;

        if (!_totp.VerifyCode(secret, request.Code, now))
        {
            return Result.Failure(TwoFactorErrors.CodigoInvalido);
        }

        employee.EnableTwoFactor(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
