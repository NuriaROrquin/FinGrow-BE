namespace FinGrow.Application.DTOs;

using FinGrow.Domain.Entities;

public sealed record DepartmentManagerResponse(Guid Id, string FullName, bool IsActive)
{
    public static DepartmentManagerResponse FromEntity(Employee employee) =>
        new(employee.Id, employee.FullName, employee.IsActive);
}
