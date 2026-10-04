namespace FinGrow.Application.DTOs;

public sealed record EmployeeProfileResponse(
    string FullName,
    string Email,
    string? PhoneNumber,
    string CompanyName,
    string? DepartmentName);
