namespace FinGrow.Api.Contracts;

public sealed record SessionResponse(
    Guid UserId,
    Guid CompanyId,
    string FullName,
    string Role,
    DateTimeOffset ExpiresAt);
