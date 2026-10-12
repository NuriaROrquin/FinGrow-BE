namespace FinGrow.Application.Features.Employees.ListEmployees;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListEmployeesQueryHandler
    : IRequestHandler<ListEmployeesQuery, Result<IReadOnlyList<CompanyEmployeeResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;

    public ListEmployeesQueryHandler(ICurrentUser currentUser, ICompanyRepository companies, IEmployeeRepository employees)
    {
        _currentUser = currentUser;
        _companies = companies;
        _employees = employees;
    }

    public async Task<Result<IReadOnlyList<CompanyEmployeeResponse>>> Handle(
        ListEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<IReadOnlyList<CompanyEmployeeResponse>>(EmployeeErrors.NoAutenticado);
        }

        var employees = await _employees.ListByCompanyAsync(companyId, cancellationToken);

        return Result.Success<IReadOnlyList<CompanyEmployeeResponse>>(employees
            .Select(employee => CompanyEmployeeResponse.FromEntity(
                employee,
                employee.DepartmentId is { } departmentId ? company.FindDepartment(departmentId) : null))
            .ToList());
    }
}
