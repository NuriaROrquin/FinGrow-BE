namespace FinGrow.Application.Features.Session;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using MediatR;

internal sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, Result<LoginResponse>>
{
    private static readonly Error SesionInvalida =
        Error.Unauthorized("Auth.SesionInvalida", "La sesion no es valida, esta vencida o fue cerrada.");

    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyRepository _companies;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public RefreshSessionCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IEmployeeRepository employees,
        ICompanyRepository companies,
        SessionIssuer sessionIssuer,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _refreshTokens = refreshTokens;
        _employees = employees;
        _companies = companies;
        _sessionIssuer = sessionIssuer;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<LoginResponse>> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result.Failure<LoginResponse>(SesionInvalida);
        }

        var now = _clock.UtcNow;
        var current = await _refreshTokens.FindByHashAsync(RefreshToken.Hash(request.RefreshToken), cancellationToken);

        if (current is null || !current.IsActive(now))
        {
            return Result.Failure<LoginResponse>(SesionInvalida);
        }

        var (fullName, isActiveAccount) = current.Role == Rol.Empresa
            ? await ResolveCompanyAsync(current.UserId, cancellationToken)
            : await ResolveEmployeeAsync(current.UserId, cancellationToken);

        if (fullName is null || !isActiveAccount)
        {
            // La cuenta fue dada de baja o borrada despues de haber emitido este refresh token:
            // lo revocamos aunque todavia no haya vencido para no dejar una sesion huerfana.
            current.Revoke(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<LoginResponse>(SesionInvalida);
        }

        current.Revoke(now);
        var session = _sessionIssuer.Issue(current.UserId, current.CompanyId, current.Role, fullName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(session);
    }

    private async Task<(string? FullName, bool IsActive)> ResolveEmployeeAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _employees.GetByIdAsync(id, cancellationToken);
        return (employee?.FullName, employee?.IsActive ?? false);
    }

    private async Task<(string? FullName, bool IsActive)> ResolveCompanyAsync(Guid id, CancellationToken cancellationToken)
    {
        var company = await _companies.GetByIdAsync(id, cancellationToken);
        return (company?.Name, company?.IsActive ?? false);
    }
}
