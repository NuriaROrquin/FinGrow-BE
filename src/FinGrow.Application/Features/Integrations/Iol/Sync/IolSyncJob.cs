namespace FinGrow.Application.Features.Integrations.Iol.Sync;

using System.Globalization;
using FinGrow.Application.Common;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class IolSyncJob : IScheduledJob
{
    public const string JobName = "iol-sync";

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<IolSyncJob> _logger;

    public IolSyncJob(
        IEmployeeIntegrationRepository integrations,
        IServiceScopeFactory scopes,
        ILogger<IolSyncJob> logger)
    {
        _integrations = integrations;
        _scopes = scopes;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Recorre las cuentas de IOL vinculadas e importa su portafolio como inversiones: crea las posiciones nuevas, "
        + "ajusta el capital si cambio la cantidad y registra la valuacion del dia si cambio el valor.";

    // Cada media hora mientras opera BYMA (11 a 18 de Argentina): fuera de ese horario el portafolio no cambia.
    public string Schedule => "*/30 14-21 * * 1-5";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var employees = (await _integrations.ListAuthorizedAsync(IntegrationProvider.Iol, cancellationToken))
            .Select(integration => integration.EmployeeId)
            .ToList();

        var synced = 0;
        var rejected = 0;
        var failures = new List<string>();

        foreach (var employeeId in employees)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var result = await sender.Send(new SyncIolPortfolioCommand(employeeId), cancellationToken);

                if (result.IsSuccess)
                {
                    synced++;
                }
                else if (result.Error.Type == ErrorType.Unavailable)
                {
                    failures.Add($"Empleado {employeeId}: {result.Error.Description}");
                }
                else
                {
                    rejected++;
                    LogRejected(_logger, employeeId, result.Error.Code, result.Error.Description);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogFailed(_logger, exception, employeeId);
                failures.Add($"Empleado {employeeId}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{employees.Count} cuenta(s) vinculada(s): {synced} sincronizada(s), {rejected} rechazada(s), {failures.Count} con error.");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "IOL no se sincronizo para el empleado {EmployeeId}: {Code} ({Description}).")]
    private static partial void LogRejected(ILogger logger, Guid employeeId, string code, string description);

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la sincronizacion de IOL del empleado {EmployeeId}.")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid employeeId);
}
