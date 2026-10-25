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

    private static Error EmailTaken(Email email) =>
        Error.Conflict("CompanyProfile.EmailTaken", $"Ya hay una cuenta con el email '{email}'.");

    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateCompanyProfileCommandHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        IEmployeeRepository employees,
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _companies = companies;
        _employees = employees;
        _users = users;
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

        var email = Email.From(request.Email);

        if (await _users.GetByEmailAsync(email, cancellationToken) is { } owner && owner.Id != company.Id)
        {
            return Result.Failure<CompanyProfileResponse>(EmailTaken(email));
        }

        company.UpdateCorporateInformation(
            request.LegalName,
            TaxId.From(request.TaxId),
            email,
            request.Industry,
            request.CorporatePhone,
            request.FiscalAddress,
            request.FoundedYear,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
