namespace FinGrow.Application.Features.Investments.AccrueInvestments;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class InvestmentAccrualsJob : IScheduledJob
{
    public const string JobName = "investment-accruals";

    private readonly IInvestmentRepository _investments;
    private readonly IServiceScopeFactory _scopes;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<InvestmentAccrualsJob> _logger;

    public InvestmentAccrualsJob(
        IInvestmentRepository investments,
        IServiceScopeFactory scopes,
        IDateTimeProvider clock,
        ILogger<InvestmentAccrualsJob> logger)
    {
        _investments = investments;
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Devenga con su TNA los plazos fijos, las cauciones y las cuentas remuneradas y les registra la valuacion "
        + "del dia; las que ya vencieron quedan valuadas al vencimiento.";

    public string Schedule => "0 6 * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var today = _clock.Today;
        var accrued = 0;
        var failures = new List<string>();

        foreach (var employeeId in await _investments.ListEmployeesWithAccruingInvestmentsAsync(cancellationToken))
        {
            try
            {
                accrued += await AccrueEmployeeAsync(employeeId, today, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogEmployeeFailed(_logger, exception, employeeId);
                failures.Add($"Empleado {employeeId}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{accrued} inversion(es) devengada(s) al {today:yyyy-MM-dd}, {failures.Count} error(es).");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    private async Task<int> AccrueEmployeeAsync(Guid employeeId, DateOnly today, CancellationToken cancellationToken)
    {
        using var scope = _scopes.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInvestmentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var investments = await repository.ListAccruingByEmployeeAsync(employeeId, cancellationToken);

        foreach (var investment in investments)
        {
            investment.Accrue(today, _clock.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return investments.Count;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo el devengamiento de las inversiones del empleado {EmployeeId}.")]
    private static partial void LogEmployeeFailed(ILogger logger, Exception exception, Guid employeeId);
}
