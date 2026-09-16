namespace FinGrow.Application.Features.Integrations.WhatsApp.UnlinkWhatsApp;

using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

internal sealed partial class UnlinkWhatsAppHandler : IRequestHandler<UnlinkWhatsAppCommand, Result>
{
    private readonly ICurrentUser _currentUser;
    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnlinkWhatsAppHandler> _logger;

    public UnlinkWhatsAppHandler(
        ICurrentUser currentUser,
        IEmployeeIntegrationRepository integrations,
        IUnitOfWork unitOfWork,
        ILogger<UnlinkWhatsAppHandler> logger)
    {
        _currentUser = currentUser;
        _integrations = integrations;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UnlinkWhatsAppCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } employeeId)
        {
            return Result.Failure(
                Error.Forbidden("Integrations.Unauthenticated", "Hay que iniciar sesion para desvincular WhatsApp."));
        }

        var integration = await _integrations.FindByEmployeeAsync(
            employeeId, IntegrationProvider.WhatsApp, cancellationToken);

        if (integration is null)
        {
            return Result.Failure(
                Error.NotFound("Integrations.NotLinked", "No tenes un numero de WhatsApp vinculado."));
        }

        // Se borra la fila entera: el numero es lo unico que guardamos, no hay credenciales aparte.
        _integrations.Remove(integration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        LogUnlinked(_logger, employeeId);

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "WhatsApp desvinculado del empleado {EmployeeId}.")]
    private static partial void LogUnlinked(ILogger logger, Guid employeeId);
}
