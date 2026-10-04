namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Session;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class VerifyTwoFactorLoginCommandHandler : IRequestHandler<VerifyTwoFactorLoginCommand, Result<LoginResponse>>
{
    private static readonly Error DesafioInvalido =
        Error.Unauthorized("Auth.DesafioInvalido", "El ingreso venció o no es válido. Volvé a iniciar sesión.");

    private static readonly Error CodigoInvalido =
        Error.Unauthorized("Auth.CodigoInvalido", "El código de verificación no es correcto.");

    private readonly ITokenService _tokenService;
    private readonly ITotpService _totp;
    private readonly IEmployeeRepository _employees;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public VerifyTwoFactorLoginCommandHandler(
        ITokenService tokenService,
        ITotpService totp,
        IEmployeeRepository employees,
        SessionIssuer sessionIssuer,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _tokenService = tokenService;
        _totp = totp;
        _employees = employees;
        _sessionIssuer = sessionIssuer;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<LoginResponse>> Handle(VerifyTwoFactorLoginCommand request, CancellationToken cancellationToken)
    {
        if (_tokenService.ReadTwoFactorChallenge(request.ChallengeToken) is not { } employeeId)
        {
            return Result.Failure<LoginResponse>(DesafioInvalido);
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null || !employee.IsActive || !employee.IsTwoFactorEnabled)
        {
            return Result.Failure<LoginResponse>(DesafioInvalido);
        }

        var now = _clock.UtcNow;

        if (!_totp.VerifyCode(employee.TwoFactorSecret!, request.Code, now))
        {
            return Result.Failure<LoginResponse>(CodigoInvalido);
        }

        employee.RegisterLogin(now);

        var session = _sessionIssuer.Issue(employee.Id, employee.CompanyId, Rol.Empleado, employee.FullName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }
}
