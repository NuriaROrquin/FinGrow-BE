namespace FinGrow.Application.Features.Integrations.UnlinkIntegration;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class UnlinkIntegrationHandler : IRequestHandler<UnlinkIntegrationCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IGmailOAuthClient _gmail;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnlinkIntegrationHandler> _logger;

    public UnlinkIntegrationHandler(
        ICurrentUser currentUser,
        IEmployeeIntegrationRepository integrations,
        IGmailOAuthClient gmail,
        IUnitOfWork unitOfWork,
        ILogger<UnlinkIntegrationHandler> logger)
    {
        _currentUser = currentUser;
        _integrations = integrations;
        _gmail = gmail;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UnlinkIntegrationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure(
                Error.Forbidden("Integrations.Unauthenticated", $"Hay que iniciar sesion para desvincular {request.Provider}."));
        }

        var integration = await _integrations.FindByEmployeeAsync(employeeId, request.Provider, cancellationToken);

        if (integration is null)
        {
            return Result.Failure(Error.NotFound("Integrations.NotLinked", $"No tenes {request.Provider} vinculado."));
        }

        await RevokeGmailAccessAsync(integration, cancellationToken);

        _integrations.Remove(integration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogUnlinked(_logger, request.Provider, employeeId);

        return Result.Success();
    }

    // Si Google no responde se desvincula igual: el empleado puede quitar el acceso desde su cuenta
    // de Google, pero no tiene otra forma de sacar la integracion de FinGrow.
    private async Task RevokeGmailAccessAsync(EmployeeIntegration integration, CancellationToken cancellationToken)
    {
        if (integration.Provider != IntegrationProvider.Gmail || integration.Grant is not { } grant)
        {
            return;
        }

        try
        {
            await _gmail.RevokeAsync(grant.RefreshToken ?? grant.AccessToken, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogRevokeFailed(_logger, exception, integration.EmployeeId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Provider} desvinculado del empleado {EmployeeId}.")]
    private static partial void LogUnlinked(ILogger logger, IntegrationProvider provider, Guid employeeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo revocar en Google el acceso a Gmail del empleado {EmployeeId}.")]
    private static partial void LogRevokeFailed(ILogger logger, Exception exception, Guid employeeId);
}
