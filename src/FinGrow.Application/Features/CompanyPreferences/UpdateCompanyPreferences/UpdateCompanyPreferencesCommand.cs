namespace FinGrow.Application.Features.CompanyPreferences.UpdateCompanyPreferences;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record UpdateCompanyPreferencesCommand(
    Theme Theme,
    Language Language,
    Currency Currency,
    int FiscalYearStartMonth) : IRequest<Result<CompanyPreferencesResponse>>;
