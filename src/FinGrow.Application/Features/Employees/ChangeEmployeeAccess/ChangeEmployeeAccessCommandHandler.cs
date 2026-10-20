namespace FinGrow.Application.Features.Employees.ChangeEmployeeAccess;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ChangeEmployeeAccessCommandHandler
    : IRequestHandler<ChangeEmployeeAccessCommand, Result<CompanyEmployeeResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public ChangeEmployeeAccessCommandHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        IEmployeeRepository employees,
        IRefreshTokenRepository refreshTokens,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _companies = companies;
        _employees = employees;
        _refreshTokens = refreshTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CompanyEmployeeResponse>> Handle(
        ChangeEmployeeAccessCommand request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<CompanyEmployeeResponse>(EmployeeErrors.NoAutenticado);
        }

        var employee = await _employees.GetByIdAsync(request.EmployeeId, cancellationToken);

        if (employee is null || employee.CompanyId != companyId)
        {
            return Result.Failure<CompanyEmployeeResponse>(EmployeeErrors.NotFound(request.EmployeeId));
        }

        if (employee.IsWithdrawn && request.IsActive)
        {
            return Result.Failure<CompanyEmployeeResponse>(EmployeeErrors.Withdrawn);
        }

        if (employee.IsActive != request.IsActive)
        {
            var now = _clock.UtcNow;

            if (request.IsActive)
            {
                employee.Activate(now);
            }
            else
            {
                employee.Deactivate(now);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!request.IsActive)
            {
                await _refreshTokens.RevokeAllForUserAsync(employee.Id, now, cancellationToken);
            }
        }

        return Result.Success(CompanyEmployeeResponse.FromEntity(
            employee,
            employee.DepartmentId is { } departmentId ? company.FindDepartment(departmentId) : null));
    }
}
