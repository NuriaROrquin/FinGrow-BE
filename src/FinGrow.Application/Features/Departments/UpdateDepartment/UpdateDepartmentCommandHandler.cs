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
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public UpdateDepartmentCommandHandler(
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

    public async Task<Result<DepartmentResponse>> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NoAutenticado);
        }

        if (company.FindDepartment(request.DepartmentId) is null)
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NotFound(request.DepartmentId));
        }

        if (company.IsDepartmentNameTaken(request.Name, request.DepartmentId))
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NameTaken(request.Name));
        }

        var department = company.UpdateDepartment(
            request.DepartmentId,
            request.Name,
            request.Description,
            request.ManagerName,
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var employees = await _employees.CountByDepartmentAsync(companyId, cancellationToken);

        return Result.Success(DepartmentResponse.FromEntity(department, employees.GetValueOrDefault(department.Id)));
    }
}
