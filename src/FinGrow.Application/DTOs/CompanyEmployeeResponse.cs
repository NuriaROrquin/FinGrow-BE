namespace FinGrow.Application.DTOs;

using FinGrow.Domain.Entities;

public sealed record CompanyEmployeeResponse(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    Guid? DepartmentId,
    string? DepartmentName,
    DateOnly HiredOn,
    bool IsActive,
    DateTimeOffset? LastLoginAt)
{
    public static CompanyEmployeeResponse FromEntity(Employee employee, Department? department) =>
        new(
            employee.Id,
            employee.FullName,
            employee.User.Email.Value,
            employee.PhoneNumber,
            employee.DepartmentId,
            department?.Name,
            employee.HiredOn,
            employee.User.IsActive,
            employee.User.LastLoginAt);
}
