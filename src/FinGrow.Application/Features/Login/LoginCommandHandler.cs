namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Session;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private static readonly Error CredencialesInvalidas =
        Error.Unauthorized("Auth.CredencialesInvalidas", "El email o la contraseña son incorrectos.");

    private static readonly Error AccesoDesactivado =
        Error.Forbidden("Auth.AccesoDesactivado", "Tu acceso a FinGrow está desactivado. Consultá con tu empresa.");

    private readonly IUserRepository _users;
    private readonly SessionAccountResolver _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionIssuer _sessionIssuer;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public LoginCommandHandler(
        IUserRepository users,
        SessionAccountResolver accounts,
        IPasswordHasher passwordHasher,
        SessionIssuer sessionIssuer,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _users = users;
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<LoginResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        Email email;
        try
        {
            email = Email.From(request.Email);
        }
        catch (Domain.Errors.DomainException)
        {
            return Result.Failure<LoginResult>(CredencialesInvalidas);
        }

        var user = await _users.GetByEmailAsync(email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<LoginResult>(CredencialesInvalidas);
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResult>(AccesoDesactivado);
        }

        if (user.IsTwoFactorEnabled)
        {
            return Result.Success(LoginResult.RequiresTwoFactor(_tokenService.GenerateTwoFactorChallenge(user.Id)));
        }

        if (await _accounts.ResolveAsync(user, cancellationToken) is not { } account)
        {
            return Result.Failure<LoginResult>(CredencialesInvalidas);
        }

        user.RegisterLogin(_clock.UtcNow);

        var session = _sessionIssuer.Issue(account.UserId, account.CompanyId, account.Role, account.FullName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(LoginResult.WithSession(session));
    }
}
