namespace FinGrow.Application.DTOs;

public sealed record CreatedEmployeeResponse(CompanyEmployeeResponse Employee, string TemporaryPassword);
