namespace FinGrow.Application.Features.Departments.CreateDepartment;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<DepartmentResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public CreateDepartmentCommandHandler(
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

    public async Task<Result<DepartmentResponse>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NoAutenticado);
        }

        if (company.IsDepartmentNameTaken(request.Name))
        {
            return Result.Failure<DepartmentResponse>(DepartmentErrors.NameTaken(request.Name));
        }

        var department = company.AddDepartment(request.Name, request.Description, request.ManagerName, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(DepartmentResponse.FromEntity(department, employeeCount: 0));
    }
}
