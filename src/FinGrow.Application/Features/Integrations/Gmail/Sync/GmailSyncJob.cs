namespace FinGrow.Application.Features.Integrations.Gmail.Sync;

using System.Globalization;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class GmailSyncJob : IScheduledJob
{
    public const string JobName = "gmail-sync";

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<GmailSyncJob> _logger;

    public GmailSyncJob(
        IEmployeeIntegrationRepository integrations,
        IServiceScopeFactory scopes,
        ILogger<GmailSyncJob> logger)
    {
        _integrations = integrations;
        _scopes = scopes;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Recorre los Gmail vinculados, le pasa a FinGrow-AI los mails que parecen facturas y propone como "
        + "pendientes las que lo son. Si Google rechaza el acceso, la integracion queda con error y se avisa al empleado.";

    public string Schedule => "0 13 * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var employees = (await _integrations.ListAuthorizedAsync(IntegrationProvider.Gmail, cancellationToken))
            .Where(integration => integration.Status == IntegrationStatus.Active)
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
                var result = await sender.Send(new SyncGmailMessagesCommand(employeeId), cancellationToken);

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
            $"{employees.Count} Gmail vinculado(s): {synced} sincronizado(s), {rejected} rechazado(s), {failures.Count} con error.");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gmail no se sincronizo para el empleado {EmployeeId}: {Code} ({Description}).")]
    private static partial void LogRejected(ILogger logger, Guid employeeId, string code, string description);

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la sincronizacion de Gmail del empleado {EmployeeId}.")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid employeeId);
}
