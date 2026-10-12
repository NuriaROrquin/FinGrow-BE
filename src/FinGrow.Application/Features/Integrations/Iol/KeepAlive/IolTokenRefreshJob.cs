namespace FinGrow.Application.Features.Integrations.Iol.KeepAlive;

using System.Globalization;
using FinGrow.Application.Common;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class IolTokenRefreshJob : IScheduledJob
{
    public const string JobName = "iol-token-refresh";

    private readonly IEmployeeIntegrationRepository _integrations;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<IolTokenRefreshJob> _logger;

    public IolTokenRefreshJob(
        IEmployeeIntegrationRepository integrations,
        IServiceScopeFactory scopes,
        ILogger<IolTokenRefreshJob> logger)
    {
        _integrations = integrations;
        _scopes = scopes;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Renueva la autorizacion de cada cuenta de IOL vinculada antes de que venza. El refresh token de IOL dura "
        + "20 minutos: si nadie lo renueva, el empleado tiene que volver a poner su contrasena.";

    // Las 24 horas: el refresh token vence aunque el mercado este cerrado.
    public string Schedule => "*/5 * * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var employees = (await _integrations.ListAuthorizedAsync(IntegrationProvider.Iol, cancellationToken))
            .Select(integration => integration.EmployeeId)
            .ToList();

        var renewed = 0;
        var current = 0;
        var expired = 0;
        var failures = new List<string>();

        foreach (var employeeId in employees)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var integrations = scope.ServiceProvider.GetRequiredService<IEmployeeIntegrationRepository>();
                var tokens = scope.ServiceProvider.GetRequiredService<IolAccessTokenProvider>();
                var integration = await integrations.FindByEmployeeAsync(employeeId, IntegrationProvider.Iol, cancellationToken);

                if (integration is null)
                {
                    continue;
                }

                var result = await tokens.KeepAliveAsync(integration, cancellationToken);

                if (result.IsSuccess)
                {
                    if (result.Value)
                    {
                        renewed++;
                    }
                    else
                    {
                        current++;
                    }
                }
                else if (result.Error.Type == ErrorType.Unavailable)
                {
                    failures.Add($"Empleado {employeeId}: {result.Error.Description}");
                }
                else
                {
                    expired++;
                    LogExpired(_logger, employeeId, result.Error.Code);
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
            $"{employees.Count} cuenta(s) de IOL: {renewed} renovada(s), {current} vigente(s), {expired} vencida(s), {failures.Count} con error.");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "La autorizacion de IOL del empleado {EmployeeId} ya no se puede renovar: {Code}.")]
    private static partial void LogExpired(ILogger logger, Guid employeeId, string code);

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la renovacion de la autorizacion de IOL del empleado {EmployeeId}.")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid employeeId);
}
