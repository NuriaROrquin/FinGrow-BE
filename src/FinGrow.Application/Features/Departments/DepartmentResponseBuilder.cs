namespace FinGrow.Application.Features.Departments;

using FinGrow.Application.DTOs;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;

internal sealed class DepartmentResponseBuilder
{
    private readonly IEmployeeRepository _employees;

    public DepartmentResponseBuilder(IEmployeeRepository employees) => _employees = employees;

    public async Task<IReadOnlyList<DepartmentResponse>> BuildAsync(
        Guid companyId,
        IReadOnlyCollection<Department> departments,
        CancellationToken cancellationToken)
    {
        var employeeCounts = await _employees.CountByDepartmentAsync(companyId, cancellationToken);
        var managerIds = departments.Select(department => department.ManagerId).OfType<Guid>().Distinct().ToList();
        var managers = managerIds.Count == 0
            ? new Dictionary<Guid, Employee>()
            : (await _employees.ListByIdsAsync(managerIds, cancellationToken)).ToDictionary(employee => employee.Id);

        return departments
            .Select(department => DepartmentResponse.FromEntity(
                department,
                employeeCounts.GetValueOrDefault(department.Id),
                department.ManagerId is { } managerId ? managers.GetValueOrDefault(managerId) : null))
            .ToList();
    }

    public async Task<DepartmentResponse> BuildAsync(
        Guid companyId,
        Department department,
        CancellationToken cancellationToken) =>
        (await BuildAsync(companyId, new[] { department }, cancellationToken))[0];
}
