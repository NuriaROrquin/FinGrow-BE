namespace FinGrow.Application.Features.CompanyPreferences.UpdateCompanyPreferences;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class UpdateCompanyPreferencesCommandHandler
    : IRequestHandler<UpdateCompanyPreferencesCommand, Result<CompanyPreferencesResponse>>
{
    private static readonly Error NoAutenticado =
        Error.Unauthorized("CompanyPreferences.NoAutenticado", "Hay que iniciar sesión para editar las preferencias de la empresa.");

    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateCompanyPreferencesCommandHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _companies = companies;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CompanyPreferencesResponse>> Handle(
        UpdateCompanyPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetByIdAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<CompanyPreferencesResponse>(NoAutenticado);
        }

        company.UpdatePreferences(
            request.Theme,
            request.Language,
            request.Currency,
            request.FiscalYearStartMonth,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CompanyPreferencesResponse(
            company.Theme,
            company.Language,
            company.DefaultCurrency,
            company.FiscalYearStartMonth));
    }
}
