namespace FinGrow.Application.DTOs;

using FinGrow.Domain.Enums;

public sealed record EmployeePreferencesResponse(
    Theme Theme,
    Language Language,
    Currency Currency,
    DateFormat DateFormat);
