namespace FinGrow.Application.Features.Integrations.Linking;

using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal enum LinkOutcome
{
    NotACode,
    InvalidCode,
    Linked
}

internal sealed record LinkAttempt(LinkOutcome Outcome, Employee? Employee = null)
{
    public static readonly LinkAttempt NotACode = new(LinkOutcome.NotACode);

    public static readonly LinkAttempt InvalidCode = new(LinkOutcome.InvalidCode);

    public static LinkAttempt Linked(Employee employee) => new(LinkOutcome.Linked, employee);
}

internal sealed partial class LinkCodeRedeemer
{
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IIntegrationLinkCodeRepository _linkCodes;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<LinkCodeRedeemer> _logger;

    public LinkCodeRedeemer(
        IEmployeeIntegrationRepository integrations,
        IIntegrationLinkCodeRepository linkCodes,
        IEmployeeRepository employees,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<LinkCodeRedeemer> logger)
    {
        _integrations = integrations;
        _linkCodes = linkCodes;
        _employees = employees;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<bool> IsRedeemableAsync(
        IntegrationProvider provider,
        string text,
        CancellationToken cancellationToken)
    {
        var code = IntegrationLinkCode.Normalize(text);

        if (code.Length != IntegrationLinkCode.Length)
        {
            return false;
        }

        var linkCode = await _linkCodes.FindByHashAsync(provider, IntegrationLinkCode.Hash(code), cancellationToken);

        return linkCode is not null && linkCode.IsUsable(_clock.UtcNow);
    }

    public async Task<LinkAttempt> TryLinkAsync(
        IntegrationProvider provider,
        string externalAccountId,
        string text,
        CancellationToken cancellationToken,
        OAuthGrant? grant = null)
    {
        var code = IntegrationLinkCode.Normalize(text);

        if (code.Length != IntegrationLinkCode.Length)
        {
            return LinkAttempt.NotACode;
        }

        var now = _clock.UtcNow;
        var linkCode = await _linkCodes.FindByHashAsync(provider, IntegrationLinkCode.Hash(code), cancellationToken);

        if (linkCode is null || !linkCode.IsUsable(now))
        {
            return LinkAttempt.InvalidCode;
        }

        var employee = await _employees.GetByIdAsync(linkCode.EmployeeId, cancellationToken);

        if (employee is null || !employee.IsActive)
        {
            return LinkAttempt.InvalidCode;
        }

        linkCode.Redeem(now);

        var integration = await _integrations.FindByEmployeeAsync(employee.Id, provider, cancellationToken);

        if (integration is null)
        {
            integration = EmployeeIntegration.Create(employee.Id, provider, externalAccountId, now);
            _integrations.Add(integration);
        }
        else
        {
            integration.Relink(externalAccountId, now);
        }

        if (grant is not null)
        {
            integration.Authorize(grant, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogLinked(_logger, provider, employee.Id);

        return LinkAttempt.Linked(employee);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Provider} vinculado al empleado {EmployeeId}.")]
    private static partial void LogLinked(ILogger logger, IntegrationProvider provider, Guid employeeId);
}
