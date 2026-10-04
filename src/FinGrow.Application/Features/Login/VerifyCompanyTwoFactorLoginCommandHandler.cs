namespace FinGrow.Application.Features.Login;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Session;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class VerifyCompanyTwoFactorLoginCommandHandler : IRequestHandler<VerifyCompanyTwoFactorLoginCommand, Result<LoginResponse>>
{
    private static readonly Error DesafioInvalido =
        Error.Unauthorized("Auth.DesafioInvalido", "El ingreso venció o no es válido. Volvé a iniciar sesión.");

    private static readonly Error CodigoInvalido =
        Error.Unauthorized("Auth.CodigoInvalido", "El código de verificación no es correcto.");

    private readonly ITokenService _tokenService;
    private readonly ITotpService _totp;
    private readonly ICompanyRepository _companies;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public VerifyCompanyTwoFactorLoginCommandHandler(
        ITokenService tokenService,
        ITotpService totp,
        ICompanyRepository companies,
        SessionIssuer sessionIssuer,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _tokenService = tokenService;
        _totp = totp;
        _companies = companies;
        _sessionIssuer = sessionIssuer;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<LoginResponse>> Handle(VerifyCompanyTwoFactorLoginCommand request, CancellationToken cancellationToken)
    {
        if (_tokenService.ReadTwoFactorChallenge(request.ChallengeToken) is not { } companyId)
        {
            return Result.Failure<LoginResponse>(DesafioInvalido);
        }

        var company = await _companies.GetByIdAsync(companyId, cancellationToken);

        if (company is null || !company.IsActive || !company.IsTwoFactorEnabled)
        {
            return Result.Failure<LoginResponse>(DesafioInvalido);
        }

        if (!_totp.VerifyCode(company.TwoFactorSecret!, request.Code, _clock.UtcNow))
        {
            return Result.Failure<LoginResponse>(CodigoInvalido);
        }

        var session = _sessionIssuer.Issue(company.Id, company.Id, Rol.Empresa, company.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }
}
