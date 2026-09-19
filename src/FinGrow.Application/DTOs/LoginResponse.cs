namespace FinGrow.Application.DTOs;

public sealed record LoginResponse(
    Guid EmployeeId,
    Guid CompanyId,
    string FullName,
    string Role,
    string Token,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt);
