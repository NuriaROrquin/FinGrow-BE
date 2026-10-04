namespace FinGrow.Application.Features.Account.Profile;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, Result<EmployeeProfileResponse>>
{
    private static readonly Error NoAutenticado =
        Error.Unauthorized("Account.NoAutenticado", "Hay que iniciar sesión para ver tu perfil.");

    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyRepository _companies;

    public GetProfileQueryHandler(ICurrentUser currentUser, IEmployeeRepository employees, ICompanyRepository companies)
    {
        _currentUser = currentUser;
        _employees = employees;
        _companies = companies;
    }

    public async Task<Result<EmployeeProfileResponse>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<EmployeeProfileResponse>(NoAutenticado);
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<EmployeeProfileResponse>(NoAutenticado);
        }

        var company = await _companies.GetWithDepartmentsAsync(employee.CompanyId, cancellationToken);

        if (company is null)
        {
            return Result.Failure<EmployeeProfileResponse>(NoAutenticado);
        }

        var department = company.Departments.FirstOrDefault(department => department.Id == employee.DepartmentId);

        return Result.Success(new EmployeeProfileResponse(
            employee.FullName,
            employee.Email.Value,
            employee.PhoneNumber,
            employee.NationalId,
            employee.BirthDate,
            employee.Address,
            company.Name,
            department?.Name));
    }
}
