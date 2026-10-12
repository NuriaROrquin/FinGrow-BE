namespace FinGrow.Application.Features.Employees.UpdateEmployee;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Result<CompanyEmployeeResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateEmployeeCommandHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        IEmployeeRepository employees,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _companies = companies;
        _employees = employees;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CompanyEmployeeResponse>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<CompanyEmployeeResponse>(EmployeeErrors.NoAutenticado);
        }

        var employee = await _employees.GetByIdAsync(request.EmployeeId, cancellationToken);

        if (employee is null || employee.CompanyId != companyId)
        {
            return Result.Failure<CompanyEmployeeResponse>(EmployeeErrors.NotFound(request.EmployeeId));
        }

        var email = Email.From(request.Email);

        if (await _employees.GetByEmailAsync(email, cancellationToken) is { } owner && owner.Id != employee.Id)
        {
            return Result.Failure<CompanyEmployeeResponse>(EmployeeErrors.EmailTaken(email));
        }

        employee.UpdateDetails(request.FullName, email, request.PhoneNumber, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CompanyEmployeeResponse.FromEntity(
            employee,
            employee.DepartmentId is { } departmentId ? company.FindDepartment(departmentId) : null));
    }
}
