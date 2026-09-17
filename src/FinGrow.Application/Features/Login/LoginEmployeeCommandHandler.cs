namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class LoginEmployeeCommandHandler : IRequestHandler<LoginEmployeeCommand, Result<LoginResponse>>
{
    private const string EmployeeRole = "Empleado";

    private static readonly Error CredencialesInvalidas =
        Error.Unauthorized("Auth.CredencialesInvalidas", "El email o la contraseña son incorrectos.");

    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginEmployeeCommandHandler(
        IEmployeeRepository employeeRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<LoginResponse>> Handle(LoginEmployeeCommand request, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.From(request.Email);
        }
        catch (Domain.Errors.DomainException)
        {
            return Result.Failure<LoginResponse>(CredencialesInvalidas);
        }

        var employee = await _employeeRepository.GetByEmailAsync(email, cancellationToken);

        if (employee is null || !_passwordHasher.Verify(request.Password, employee.PasswordHash))
        {
            return Result.Failure<LoginResponse>(CredencialesInvalidas);
        }

        if (!employee.IsActive)
        {
            return Result.Failure<LoginResponse>(CredencialesInvalidas);
        }

        employee.RegisterLogin(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _tokenService.GenerateToken(employee.Id, employee.CompanyId, Rol.Empleado, employee.FullName);

        return Result.Success(new LoginResponse(
            employee.Id,
            employee.CompanyId,
            employee.FullName,
            Rol.Empleado,
            token.Value,
            token.ExpiresAt));
    }
}
