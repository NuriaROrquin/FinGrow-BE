namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Session;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using MediatR;

internal sealed class LoginCompanyCommandHandler : IRequestHandler<LoginCompanyCommand, Result<LoginResponse>>
{
    private static readonly Error CredencialesInvalidas =
        Error.Unauthorized("Auth.CredencialesInvalidas", "El email o la contraseña son incorrectos.");

    private readonly ICompanyRepository _companyRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCompanyCommandHandler(
        ICompanyRepository companyRepository,
        IPasswordHasher passwordHasher,
        SessionIssuer sessionIssuer,
        IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _passwordHasher = passwordHasher;
        _sessionIssuer = sessionIssuer;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponse>> Handle(
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
            return Result.Failure<LoginResponse>(CredencialesInvalidas);
        }

        var company = await _companyRepository.GetByEmailAsync(email, cancellationToken);

        if (company is null || !_passwordHasher.Verify(request.Password, company.PasswordHash))
        {
            return Result.Failure<LoginResponse>(CredencialesInvalidas);
        }

        if (!company.IsActive)
        {
            return Result.Failure<LoginResponse>(CredencialesInvalidas);
        }

        var session = _sessionIssuer.Issue(company.Id, company.Id, Rol.Empresa, company.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }
}
