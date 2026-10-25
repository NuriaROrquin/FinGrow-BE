namespace FinGrow.Application.Features.Session;

using FinGrow.Application.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;

internal sealed class SessionAccountResolver
{
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyRepository _companies;

    public SessionAccountResolver(IEmployeeRepository employees, ICompanyRepository companies)
    {
        _employees = employees;
        _companies = companies;
    }

    public async Task<SessionAccount?> ResolveAsync(User user, CancellationToken cancellationToken)
    {
        if (user.Role == UserRole.Company)
        {
            return await _companies.GetByIdAsync(user.Id, cancellationToken) is { } company
                ? new SessionAccount(company.Id, company.Id, Rol.Empresa, company.Name)
                : null;
        }

        return await _employees.GetByIdAsync(user.Id, cancellationToken) is { } employee
            ? new SessionAccount(employee.Id, employee.CompanyId, Rol.Empleado, employee.FullName)
            : null;
    }
}
