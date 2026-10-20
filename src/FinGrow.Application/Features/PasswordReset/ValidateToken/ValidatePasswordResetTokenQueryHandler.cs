namespace FinGrow.Application.Features.PasswordReset.ValidateToken;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ValidatePasswordResetTokenQueryHandler
    : IRequestHandler<ValidatePasswordResetTokenQuery, Result<PasswordResetResponse>>
{
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyRepository _companies;
    private readonly IDateTimeProvider _clock;

    public ValidatePasswordResetTokenQueryHandler(
        IPasswordResetTokenRepository tokens,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        IDateTimeProvider clock)
    {
        _tokens = tokens;
        _employees = employees;
        _companies = companies;
        _clock = clock;
    }

    public async Task<Result<PasswordResetResponse>> Handle(
        ValidatePasswordResetTokenQuery request,
        CancellationToken cancellationToken)
    {
        var token = string.IsNullOrWhiteSpace(request.Token)
            ? null
            : await _tokens.FindByHashAsync(PasswordResetToken.Hash(request.Token), cancellationToken);

        if (token is null || !token.IsUsable(_clock.UtcNow)
            || await ResettableAccount.FindByIdAsync(token.Role, token.UserId, _employees, _companies, cancellationToken) is null)
        {
            return Result.Failure<PasswordResetResponse>(PasswordResetErrors.EnlaceInvalido);
        }

        return Result.Success(new PasswordResetResponse(token.Role));
    }
}
