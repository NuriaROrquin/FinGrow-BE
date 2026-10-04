namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Session;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class LoginCompanyCommandHandler : IRequestHandler<LoginCompanyCommand, Result<LoginResult>>
{
    private static readonly Error CredencialesInvalidas =
        Error.Unauthorized("Auth.CredencialesInvalidas", "El email o la contraseña son incorrectos.");

    private readonly ICompanyRepository _companyRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionIssuer _sessionIssuer;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCompanyCommandHandler(
        ICompanyRepository companyRepository,
        IPasswordHasher passwordHasher,
        SessionIssuer sessionIssuer,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResult>> Handle(
        LoginCompanyCommand request,
        CancellationToken cancellationToken)
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

        var company = await _companyRepository.GetByEmailAsync(email, cancellationToken);

        if (company is null || !_passwordHasher.Verify(request.Password, company.PasswordHash))
        {
            return Result.Failure<LoginResult>(CredencialesInvalidas);
        }

        if (!company.IsActive)
        {
            return Result.Failure<LoginResult>(CredencialesInvalidas);
        }

        if (company.IsTwoFactorEnabled)
        {
            return Result.Success(LoginResult.RequiresTwoFactor(_tokenService.GenerateTwoFactorChallenge(company.Id)));
        }

        var session = _sessionIssuer.Issue(company.Id, company.Id, Rol.Empresa, company.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(LoginResult.WithSession(session));
    }
}
