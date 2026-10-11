namespace FinGrow.Application.DTOs;

using FinGrow.Domain.Entities;

public sealed record DepartmentResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ManagerName,
    int EmployeeCount,
    bool IsActive,
    DateTimeOffset CreatedAt)
{
    public static DepartmentResponse FromEntity(Department department, int employeeCount) =>
        new(
            department.Id,
            department.Name,
            department.Description,
            department.ManagerName,
            employeeCount,
            department.IsActive,
            department.CreatedAt);
}
