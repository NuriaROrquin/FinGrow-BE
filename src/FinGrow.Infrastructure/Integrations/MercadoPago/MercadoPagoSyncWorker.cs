namespace FinGrow.Infrastructure.Integrations.MercadoPago;

using FinGrow.Application.Features.Integrations.MercadoPago.Sync;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed partial class MercadoPagoSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly MercadoPagoOptions _options;
    private readonly ILogger<MercadoPagoSyncWorker> _logger;

    public MercadoPagoSyncWorker(
        IServiceScopeFactory scopes,
        IOptions<MercadoPagoOptions> options,
        ILogger<MercadoPagoSyncWorker> logger)
    {
        _scopes = scopes;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.SyncInitialDelaySeconds), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.SyncIntervalMinutes));

            do
            {
                await SyncEveryoneAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task SyncEveryoneAsync(CancellationToken cancellationToken)
    {
        List<Guid> employees;

        try
        {
            using var scope = _scopes.CreateScope();
            var integrations = scope.ServiceProvider.GetRequiredService<IEmployeeIntegrationRepository>();

            employees = (await integrations.ListAuthorizedAsync(IntegrationProvider.MercadoPago, cancellationToken))
                .Select(integration => integration.EmployeeId)
                .ToList();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogListingFailed(_logger, exception);

            return;
        }

        LogCycleStarted(_logger, employees.Count);

        foreach (var employeeId in employees)
        {
            await SyncOneAsync(employeeId, cancellationToken);
        }
    }

    private async Task SyncOneAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var result = await sender.Send(new SyncMercadoPagoMovementsCommand(employeeId), cancellationToken);

            if (result.IsFailure)
            {
                LogSyncRejected(_logger, employeeId, result.Error.Code, result.Error.Description);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogSyncFailed(_logger, exception, employeeId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sincronizacion de Mercado Pago: {Count} cuentas vinculadas.")]
    private static partial void LogCycleStarted(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "No se pudieron listar las cuentas de Mercado Pago a sincronizar.")]
    private static partial void LogListingFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Mercado Pago no se sincronizo para el empleado {EmployeeId}: {Code} ({Description}).")]
    private static partial void LogSyncRejected(ILogger logger, Guid employeeId, string code, string description);

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la sincronizacion de Mercado Pago del empleado {EmployeeId}.")]
    private static partial void LogSyncFailed(ILogger logger, Exception exception, Guid employeeId);
}
