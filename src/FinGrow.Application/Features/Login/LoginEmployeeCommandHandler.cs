namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Session;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class LoginEmployeeCommandHandler : IRequestHandler<LoginEmployeeCommand, Result<LoginResult>>
{
    private static readonly Error CredencialesInvalidas =
        Error.Unauthorized("Auth.CredencialesInvalidas", "El email o la contraseña son incorrectos.");

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionIssuer _sessionIssuer;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IPasswordHasher passwordHasher,
        SessionIssuer sessionIssuer,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LoginResult>> Handle(LoginEmployeeCommand request, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.From(request.Email);
        }
        catch (Domain.Errors.DomainException)
        {
            return Result.Failure<LoginResult>(CredencialesInvalidas);
        }

        var employee = await _employeeRepository.GetByEmailAsync(email, cancellationToken);

        if (employee is null || !_passwordHasher.Verify(request.Password, employee.PasswordHash))
        {
            return Result.Failure<LoginResult>(CredencialesInvalidas);
        }

        if (!employee.IsActive)
        {
            return Result.Failure<LoginResult>(CredencialesInvalidas);
        }

        if (employee.IsTwoFactorEnabled)
        {
            return Result.Success(LoginResult.RequiresTwoFactor(_tokenService.GenerateTwoFactorChallenge(employee.Id)));
        }

        employee.RegisterLogin(_dateTimeProvider.UtcNow);

        var session = _sessionIssuer.Issue(employee.Id, employee.CompanyId, Rol.Empleado, employee.FullName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(LoginResult.WithSession(session));
    }
}
