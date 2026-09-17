namespace FinGrow.Application.Features.Integrations.UnlinkIntegration;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class UnlinkIntegrationHandler : IRequestHandler<UnlinkIntegrationCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnlinkIntegrationHandler> _logger;

    public UnlinkIntegrationHandler(
        ICurrentUser currentUser,
        IEmployeeIntegrationRepository integrations,
        IUnitOfWork unitOfWork,
        ILogger<UnlinkIntegrationHandler> logger)
    {
        _currentUser = currentUser;
        _integrations = integrations;
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

        _integrations.Remove(integration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogUnlinked(_logger, request.Provider, employeeId);

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Provider} desvinculado del empleado {EmployeeId}.")]
    private static partial void LogUnlinked(ILogger logger, IntegrationProvider provider, Guid employeeId);
}
