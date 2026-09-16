namespace FinGrow.Application.Features.Integrations.Linking;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;

internal sealed record IssuedLinkCode(string Code, DateTimeOffset ExpiresAt);

internal sealed class LinkCodeIssuer
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeRepository _employees;
    private readonly IIntegrationLinkCodeRepository _linkCodes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    public LinkCodeIssuer(
        ICurrentUser currentUser,
        IEmployeeRepository employees,
        IIntegrationLinkCodeRepository linkCodes,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _currentUser = currentUser;
        _employees = employees;
        _linkCodes = linkCodes;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<IssuedLinkCode>> IssueForCurrentEmployeeAsync(
        IntegrationProvider provider,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure<IssuedLinkCode>(
                Error.Forbidden("Integrations.Unauthenticated", $"Hay que iniciar sesion para vincular {provider}."));
        }

        var employee = await _employees.GetByIdAsync(employeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            return Result.Failure<IssuedLinkCode>(
                Error.Forbidden("Integrations.NotAnEmployee", $"Solo un empleado activo puede vincular {provider}."));
        }

        var code = IntegrationLinkCode.GenerateCode();
        var linkCode = IntegrationLinkCode.Create(employee.Id, provider, code, _clock.UtcNow);

        _linkCodes.Add(linkCode);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new IssuedLinkCode(code, linkCode.ExpiresAt));
    }
}
