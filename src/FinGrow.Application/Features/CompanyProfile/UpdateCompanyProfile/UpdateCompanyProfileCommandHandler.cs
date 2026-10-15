namespace FinGrow.Application.Features.CompanyProfile.UpdateCompanyProfile;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class UpdateCompanyProfileCommandHandler
    : IRequestHandler<UpdateCompanyProfileCommand, Result<CompanyProfileResponse>>
{
    private static readonly Error NoAutenticado =
        Error.Unauthorized("CompanyProfile.NoAutenticado", "Hay que iniciar sesión para editar los datos de la empresa.");

    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateCompanyProfileCommandHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        IEmployeeRepository employees,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _companies = companies;
        _employees = employees;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CompanyProfileResponse>> Handle(
        UpdateCompanyProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetByIdAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<CompanyProfileResponse>(NoAutenticado);
        }

        company.UpdateCorporateInformation(
            request.LegalName,
            TaxId.From(request.TaxId),
            Email.From(request.Email),
            request.Industry,
            request.CorporatePhone,
            request.FiscalAddress,
            request.FoundedYear,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var employeeCount = (await _employees.ListByCompanyAsync(companyId, cancellationToken))
            .Count(employee => employee.IsActive);

        return Result.Success(new CompanyProfileResponse(
            company.Name,
            company.TaxId.Value,
            company.Email.Value,
            company.Industry,
            company.PhoneNumber,
            company.FiscalAddress,
            company.FoundedYear,
            employeeCount));
    }
}
