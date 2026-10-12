namespace FinGrow.Application.Features.Departments.UpdateDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Result<DepartmentResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly DepartmentManagerEligibility _managers;
    private readonly DepartmentResponseBuilder _responses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateDepartmentCommandHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        DepartmentManagerEligibility managers,
        DepartmentResponseBuilder responses,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _companies = companies;
        _managers = managers;
        _responses = responses;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<DepartmentResponse>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NoAutenticado);
        }

        if (company.FindDepartment(request.DepartmentId) is not { } current)
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NotFound(request.DepartmentId));
        }

        if (company.IsDepartmentNameTaken(request.Name, request.DepartmentId))
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NameTaken(request.Name));
        }

        if (request.ManagerId is { } managerId
            && managerId != current.ManagerId
            && !await _managers.IsEligibleAsync(managerId, companyId, cancellationToken))
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.ManagerNotAvailable);
        }

        var department = company.UpdateDepartment(
            request.DepartmentId,
            request.Name,
            request.Description,
            request.ManagerId,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await _responses.BuildAsync(companyId, department, cancellationToken));
    }
}
