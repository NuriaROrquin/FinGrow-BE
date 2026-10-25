namespace FinGrow.Application.Features.Departments;

using FinGrow.Domain.Repositories;

internal sealed class DepartmentManagerEligibility
{
    private readonly IEmployeeRepository _employees;

    public DepartmentManagerEligibility(IEmployeeRepository employees) => _employees = employees;

    public async Task<bool> IsEligibleAsync(Guid employeeId, Guid companyId, CancellationToken cancellationToken) =>
        await _employees.GetByIdAsync(employeeId, cancellationToken) is { User.IsActive: true } employee
        && employee.CompanyId == companyId;
}
