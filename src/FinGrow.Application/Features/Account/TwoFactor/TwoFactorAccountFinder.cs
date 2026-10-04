namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Common;
using FinGrow.Domain.Repositories;

internal sealed class TwoFactorAccountFinder
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyRepository _companies;

    public TwoFactorAccountFinder(ICurrentUser currentUser, IEmployeeRepository employees, ICompanyRepository companies)
    {
        _currentUser = currentUser;
        _employees = employees;
        _companies = companies;
    }

    public async Task<ITwoFactorAccount?> FindCurrentAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
        {
            return null;
        }

        if (_currentUser.Role == Rol.Empresa)
        {
            return await _companies.GetByIdAsync(userId, cancellationToken);
        }

        return await _employees.GetByIdAsync(userId, cancellationToken);
    }
}
