namespace FinGrow.Application.DTOs;

public sealed record EmployeeProfileResponse(
    string FullName,
    string Email,
    string? PhoneNumber,
    string? NationalId,
    DateOnly? BirthDate,
    string? Address,
    string CompanyName,
    string? DepartmentName);
