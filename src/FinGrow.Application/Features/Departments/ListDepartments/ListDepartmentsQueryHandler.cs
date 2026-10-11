namespace FinGrow.Application.Features.Departments.ListDepartments;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ListDepartmentsQueryHandler
    : IRequestHandler<ListDepartmentsQuery, Result<IReadOnlyList<DepartmentResponse>>>
{
    private readonly ICurrentUser _currentUser;
    private readonly ICompanyRepository _companies;
    private readonly DepartmentResponseBuilder _responses;

    public ListDepartmentsQueryHandler(
        ICurrentUser currentUser,
        ICompanyRepository companies,
        DepartmentResponseBuilder responses)
    {
        _currentUser = currentUser;
        _companies = companies;
        _responses = responses;
    }

    public async Task<Result<IReadOnlyList<DepartmentResponse>>> Handle(
        ListDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.CompanyId is not { } companyId
            || await _companies.GetWithDepartmentsAsync(companyId, cancellationToken) is not { } company)
        {
            return Result.Failure<IReadOnlyList<DepartmentResponse>>(DepartmentErrors.NoAutenticado);
        }

        var departments = company.Departments
            .OrderBy(department => department.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result.Success(await _responses.BuildAsync(companyId, departments, cancellationToken));
    }
}
