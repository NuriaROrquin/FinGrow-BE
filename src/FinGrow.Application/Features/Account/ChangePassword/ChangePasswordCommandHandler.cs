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
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyRepository _companies;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ChangePasswordCommandHandler(
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        IPasswordHasher passwordHasher,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _employees = employees;
        _companies = companies;
        _passwordHasher = passwordHasher;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId || _currentUser.Role is not { } role)
        {
            return Result.Failure(NoAutenticado);
        }

        var now = _clock.UtcNow;

        var verified = role == Rol.Empresa
            ? await ChangeCompanyPasswordAsync(userId, request, now, cancellationToken)
            : await ChangeEmployeePasswordAsync(userId, request, now, cancellationToken);

        if (!verified)
        {
            return Result.Failure(ContrasenaActualIncorrecta);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(userId, now, cancellationToken);

        return Result.Success();
    }

    private async Task<bool> ChangeEmployeePasswordAsync(
        Guid employeeId,
        ChangePasswordCommand request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null || !_passwordHasher.Verify(request.CurrentPassword, employee.PasswordHash))
        {
            return false;
        }

        employee.ChangePassword(_passwordHasher.Hash(request.NewPassword), now);
        return true;
    }

    private async Task<bool> ChangeCompanyPasswordAsync(
        Guid companyId,
        ChangePasswordCommand request,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var company = await _companies.GetByIdAsync(companyId, cancellationToken);

        if (company is null || !_passwordHasher.Verify(request.CurrentPassword, company.PasswordHash))
        {
            return false;
        }

        company.ChangePassword(_passwordHasher.Hash(request.NewPassword), now);
        return true;
    }
}
