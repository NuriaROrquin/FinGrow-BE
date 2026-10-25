namespace FinGrow.Application.Features.PasswordReset.ValidateToken;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class ValidatePasswordResetTokenQueryHandler
    : IRequestHandler<ValidatePasswordResetTokenQuery, Result>
{
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IDateTimeProvider _clock;

    public ValidatePasswordResetTokenQueryHandler(
        IPasswordResetTokenRepository tokens,
        IUserRepository users,
        IDateTimeProvider clock)
    {
        _tokens = tokens;
        _users = users;
        _clock = clock;
    }

    public async Task<Result> Handle(
        ValidatePasswordResetTokenQuery request,
        CancellationToken cancellationToken)
    {
        var token = string.IsNullOrWhiteSpace(request.Token)
            ? null
            : await _tokens.FindByHashAsync(PasswordResetToken.Hash(request.Token), cancellationToken);

        if (token is null || !token.IsUsable(_clock.UtcNow)
            || await _users.GetByIdAsync(token.UserId, cancellationToken) is not { IsActive: true })
        {
            return Result.Failure(PasswordResetErrors.EnlaceInvalido);
        }

        return Result.Success();
    }
}
