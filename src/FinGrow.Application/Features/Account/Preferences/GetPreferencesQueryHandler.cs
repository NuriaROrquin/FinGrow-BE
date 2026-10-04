namespace FinGrow.Application.Features.Account.Preferences;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetPreferencesQueryHandler : IRequestHandler<GetPreferencesQuery, Result<EmployeePreferencesResponse>>
{
    private static readonly Error NoAutenticado =
        Error.Unauthorized("Account.NoAutenticado", "Hay que iniciar sesión para ver tus preferencias.");

    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;

    public GetPreferencesQueryHandler(ICurrentUser currentUser, IEmployeeRepository employees)
    {
        _currentUser = currentUser;
        _employees = employees;
    }

    public async Task<Result<EmployeePreferencesResponse>> Handle(GetPreferencesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<EmployeePreferencesResponse>(NoAutenticado);
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        return employee is null
            ? Result.Failure<EmployeePreferencesResponse>(NoAutenticado)
            : Result.Success(new EmployeePreferencesResponse(
                employee.Theme,
                employee.Language,
                employee.PreferredCurrency,
                employee.DateFormat));
    }
}
