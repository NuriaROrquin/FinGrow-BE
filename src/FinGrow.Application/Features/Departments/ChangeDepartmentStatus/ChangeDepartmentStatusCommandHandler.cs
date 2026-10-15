namespace FinGrow.Application.Features.Departments.ChangeDepartmentStatus;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ChangeDepartmentStatusCommandHandler
    : IRequestHandler<ChangeDepartmentStatusCommand, Result<DepartmentResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly DepartmentResponseBuilder _responses;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ChangeDepartmentStatusCommandHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        IEmployeeRepository employees,
        DepartmentResponseBuilder responses,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _companies = companies;
        _employees = employees;
        _responses = responses;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<DepartmentResponse>> Handle(
        ChangeDepartmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NoAutenticado);
        }

        if (company.FindDepartment(request.DepartmentId) is not { } department)
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NotFound(request.DepartmentId));
        }

        if (department.IsActive == request.IsActive)
        {
            return Result.Success(await _responses.BuildAsync(companyId, department, cancellationToken));
        }

        var now = _clock.UtcNow;

        if (request.IsActive)
        {
            company.ActivateDepartment(department.Id, now);
        }
        else
        {
            if (request.MoveEmployeesTo is { } targetId && !IsValidMoveTarget(company, department, targetId))
            {
                return Result.Failure<DepartmentResponse>(DepartmentErrors.InvalidMoveTarget);
            }

            var assigned = await _employees.ListByDepartmentAsync(department.Id, cancellationToken);

            foreach (var employee in assigned)
            {
                employee.AssignToDepartment(request.MoveEmployeesTo, now);
            }

            company.DeactivateDepartment(department.Id, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(await _responses.BuildAsync(companyId, department, cancellationToken));
    }

    private static bool IsValidMoveTarget(Company company, Department department, Guid targetId) =>
        targetId != department.Id && company.FindDepartment(targetId) is { IsActive: true };
}
