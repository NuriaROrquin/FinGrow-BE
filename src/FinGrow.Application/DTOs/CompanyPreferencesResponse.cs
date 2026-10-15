namespace FinGrow.Application.DTOs;

using FinGrow.Domain.Enums;

/// <summary>Preferencias generales de la empresa para su panel corporativo.</summary>
public sealed record CompanyPreferencesResponse(
    Theme Theme,
    Language Language,
    Currency Currency,
    int FiscalYearStartMonth);
