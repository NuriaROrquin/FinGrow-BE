namespace FinGrow.Application.Features.CompanyPreferences.GetCompanyPreferences;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetCompanyPreferencesQueryHandler
    : IRequestHandler<GetCompanyPreferencesQuery, Result<CompanyPreferencesResponse>>
{
    private static readonly Error NoAutenticado =
        Error.Unauthorized("CompanyPreferences.NoAutenticado", "Hay que iniciar sesión para ver las preferencias de la empresa.");

    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;

    public GetCompanyPreferencesQueryHandler(ICurrentUser currentUser, ICompanyRepository companies)
    {
        _currentUser = currentUser;
        _companies = companies;
    }

    public async Task<Result<CompanyPreferencesResponse>> Handle(
        GetCompanyPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetByIdAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<CompanyPreferencesResponse>(NoAutenticado);
        }

        return Result.Success(new CompanyPreferencesResponse(
            company.Theme,
            company.Language,
            company.DefaultCurrency,
            company.FiscalYearStartMonth));
    }
}
