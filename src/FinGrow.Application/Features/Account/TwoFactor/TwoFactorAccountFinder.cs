namespace FinGrow.Application.Features.Account.TwoFactor;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;

internal sealed class TwoFactorAccountFinder
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;

    public TwoFactorAccountFinder(ICurrentUser currentUser, IUserRepository users)
    {
        _currentUser = currentUser;
        _users = users;
    }

    public async Task<User?> FindCurrentAsync(CancellationToken cancellationToken) =>
        _currentUser.UserId is { } userId
            ? await _users.GetByIdAsync(userId, cancellationToken)
            : null;
}
