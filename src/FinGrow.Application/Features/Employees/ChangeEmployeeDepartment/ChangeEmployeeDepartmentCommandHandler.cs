namespace FinGrow.Application.Features.Employees.ChangeEmployeeDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ChangeEmployeeDepartmentCommandHandler
    : IRequestHandler<ChangeEmployeeDepartmentCommand, Result<CompanyEmployeeResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ChangeEmployeeDepartmentCommandHandler(
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

    public async Task<Result<CompanyEmployeeResponse>> Handle(
        ChangeEmployeeDepartmentCommand request,
        CancellationToken cancellationToken)
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

        var department = request.DepartmentId is { } departmentId ? company.FindDepartment(departmentId) : null;

        if (request.DepartmentId is not null
            && request.DepartmentId != employee.DepartmentId
            && department is not { IsActive: true })
        {
            return Result.Failure<CompanyEmployeeResponse>(EmployeeErrors.DepartmentNotAvailable);
        }

        employee.AssignToDepartment(request.DepartmentId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CompanyEmployeeResponse.FromEntity(employee, department));
    }
}
