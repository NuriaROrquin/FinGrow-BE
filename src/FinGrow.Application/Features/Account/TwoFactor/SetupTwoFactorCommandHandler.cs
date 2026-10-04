namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class SetupTwoFactorCommandHandler : IRequestHandler<SetupTwoFactorCommand, Result<TwoFactorSetupResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly ITotpService _totp;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public SetupTwoFactorCommandHandler(
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

    public async Task<Result<TwoFactorSetupResponse>> Handle(SetupTwoFactorCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<TwoFactorSetupResponse>(TwoFactorErrors.NoAutenticado);
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<TwoFactorSetupResponse>(TwoFactorErrors.NoAutenticado);
        }

        if (employee.IsTwoFactorEnabled)
        {
            return Result.Failure<TwoFactorSetupResponse>(TwoFactorErrors.YaActivo);
        }

        var secret = _totp.GenerateSecret();
        employee.StartTwoFactorEnrollment(secret, _clock.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new TwoFactorSetupResponse(secret, _totp.BuildProvisioningUri(secret, employee.Email.Value)));
    }
}
