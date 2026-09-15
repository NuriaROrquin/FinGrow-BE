namespace FinGrow.Application.DTOs;

public sealed record LoginResponse(Guid EmployeeId, string FullName, string Token);
