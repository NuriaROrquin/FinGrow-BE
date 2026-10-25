namespace FinGrow.Application.Features.CompanyProfile.GetCompanyProfile;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetCompanyProfileQueryHandler : IRequestHandler<GetCompanyProfileQuery, Result<CompanyProfileResponse>>
{
    private static readonly Error NoAutenticado =
        Error.Unauthorized("CompanyProfile.NoAutenticado", "Hay que iniciar sesión para ver los datos de la empresa.");

    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;

    public GetCompanyProfileQueryHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        IEmployeeRepository employees)
    {
        _currentUser = currentUser;
        _companies = companies;
        _employees = employees;
    }

    public async Task<Result<CompanyProfileResponse>> Handle(
        GetCompanyProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetByIdAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<CompanyProfileResponse>(NoAutenticado);
        }

        var employeeCount = (await _employees.ListByCompanyAsync(companyId, cancellationToken))
            .Count(employee => employee.User.IsActive);

        return Result.Success(new CompanyProfileResponse(
            company.Name,
            company.TaxId.Value,
            company.User.Email.Value,
            company.Industry,
            company.PhoneNumber,
            company.FiscalAddress,
            company.FoundedYear,
            employeeCount));
    }
}
