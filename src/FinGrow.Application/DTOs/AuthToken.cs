namespace FinGrow.Application.DTOs;

public sealed record AuthToken(string Value, DateTimeOffset ExpiresAt);
