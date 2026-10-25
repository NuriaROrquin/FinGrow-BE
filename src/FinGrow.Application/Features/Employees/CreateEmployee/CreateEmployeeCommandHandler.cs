namespace FinGrow.Application.Features.Employees.CreateEmployee;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<CreatedEmployeeResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;
    private readonly IOutboundEmailRepository _outboundEmails;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CreateEmployeeCommandHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        IEmployeeRepository employees,
        IUserRepository users,
        IOutboundEmailRepository outboundEmails,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _companies = companies;
        _employees = employees;
        _users = users;
        _outboundEmails = outboundEmails;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CreatedEmployeeResponse>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<CreatedEmployeeResponse>(EmployeeErrors.NoAutenticado);
        }

        var email = Email.From(request.Email);

        if (await _users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            return Result.Failure<CreatedEmployeeResponse>(EmployeeErrors.EmailTaken(email));
        }

        var department = request.DepartmentId is { } departmentId ? company.FindDepartment(departmentId) : null;

        if (request.DepartmentId is not null && department is not { IsActive: true })
        {
            return Result.Failure<CreatedEmployeeResponse>(EmployeeErrors.DepartmentNotAvailable);
        }

        var temporaryPassword = TemporaryPassword.Generate();
        var now = _clock.UtcNow;
        var employee = Employee.Create(
            companyId,
            department?.Id,
            request.FullName,
            email,
            request.PhoneNumber,
            _passwordHasher.Hash(temporaryPassword),
            company.DefaultCurrency,
            _clock.Today,
            now);

        _employees.Add(employee);
        _outboundEmails.Add(WelcomeEmail.Queue(employee, company, temporaryPassword, now));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreatedEmployeeResponse(
            CompanyEmployeeResponse.FromEntity(employee, department),
            temporaryPassword));
    }
}
