namespace FinGrow.Application.DTOs;

using FinGrow.Domain.Entities;

public sealed record DepartmentResponse(
    Guid Id,
    string Name,
    string? Description,
    DepartmentManagerResponse? Manager,
    int EmployeeCount,
    bool IsActive,
    DateTimeOffset CreatedAt)
{
    public static DepartmentResponse FromEntity(Department department, int employeeCount, Employee? manager) =>
        new(
            department.Id,
            department.Name,
            department.Description,
            manager is null ? null : DepartmentManagerResponse.FromEntity(manager),
            employeeCount,
            department.IsActive,
            department.CreatedAt);
}
