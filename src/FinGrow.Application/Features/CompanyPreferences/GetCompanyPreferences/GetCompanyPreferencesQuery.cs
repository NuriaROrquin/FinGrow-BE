namespace FinGrow.Application.Features.CompanyPreferences.GetCompanyPreferences;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using MediatR;

public sealed record GetCompanyPreferencesQuery : IRequest<Result<CompanyPreferencesResponse>>;
