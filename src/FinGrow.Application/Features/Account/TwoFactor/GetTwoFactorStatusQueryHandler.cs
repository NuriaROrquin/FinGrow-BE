namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class GetTwoFactorStatusQueryHandler : IRequestHandler<GetTwoFactorStatusQuery, Result<TwoFactorStatusResponse>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;

    public GetTwoFactorStatusQueryHandler(ICurrentUser currentUser, IEmployeeRepository employees)
    {
        _currentUser = currentUser;
        _employees = employees;
    }

    public async Task<Result<TwoFactorStatusResponse>> Handle(GetTwoFactorStatusQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<TwoFactorStatusResponse>(TwoFactorErrors.NoAutenticado);
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        return employee is null
            ? Result.Failure<TwoFactorStatusResponse>(TwoFactorErrors.NoAutenticado)
            : Result.Success(new TwoFactorStatusResponse(employee.IsTwoFactorEnabled));
    }
}
