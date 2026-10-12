namespace FinGrow.Application.Features.Departments.DeleteDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public DeleteDepartmentCommandHandler(
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

    public async Task<Result> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure(DepartmentErrors.NoAutenticado);
        }

        if (company.FindDepartment(request.DepartmentId) is null)
        {
            return Result.Failure(DepartmentErrors.NotFound(request.DepartmentId));
        }

        var employees = await _employees.CountByDepartmentAsync(companyId, cancellationToken);

        if (employees.GetValueOrDefault(request.DepartmentId) is var assigned and > 0)
        {
            return Result.Failure(DepartmentErrors.HasEmployees(assigned));
        }

        company.RemoveDepartment(request.DepartmentId, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
