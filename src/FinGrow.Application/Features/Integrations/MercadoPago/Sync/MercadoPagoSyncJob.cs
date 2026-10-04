namespace FinGrow.Application.Features.Integrations.MercadoPago.Sync;

using System.Globalization;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class MercadoPagoSyncJob : IScheduledJob
{
    public const string JobName = "mercadopago-sync";

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<MercadoPagoSyncJob> _logger;

    public MercadoPagoSyncJob(
        IEmployeeIntegrationRepository integrations,
        IServiceScopeFactory scopes,
        ILogger<MercadoPagoSyncJob> logger)
    {
        _integrations = integrations;
        _scopes = scopes;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Recorre las cuentas de Mercado Pago vinculadas y trae los movimientos nuevos como pendientes de revision. "
        + "Mercado Pago no avisa por webhook lo que un usuario paga, por eso hay que consultarlo.";

    public string Schedule => "0 * * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var employees = (await _integrations.ListAuthorizedAsync(IntegrationProvider.MercadoPago, cancellationToken))
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
                var result = await sender.Send(new SyncMercadoPagoMovementsCommand(employeeId), cancellationToken);

                if (result.IsSuccess)
                {
                    synced++;
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Mercado Pago no se sincronizo para el empleado {EmployeeId}: {Code} ({Description}).")]
    private static partial void LogRejected(ILogger logger, Guid employeeId, string code, string description);

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la sincronizacion de Mercado Pago del empleado {EmployeeId}.")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid employeeId);
}
