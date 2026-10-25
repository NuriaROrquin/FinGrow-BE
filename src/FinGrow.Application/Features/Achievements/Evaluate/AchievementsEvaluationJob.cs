namespace FinGrow.Application.Features.Achievements.Evaluate;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Otorga los logros de mes cerrado y hace de red de seguridad de los instantaneos. En la primera
/// corrida exitosa los instantaneos que ya se cumplian se otorgan para celebrarlos juntos; despues,
/// lo que encuentre es un otorgamiento en vivo que fallo y se registra sin celebrar.
/// </summary>
internal sealed partial class AchievementsEvaluationJob(
    ICompanyRepository companies,
    IEmployeeRepository employees,
    IJobRunRepository jobRuns,
    MonthlyAchievementsEvaluator monthlyEvaluator,
    IServiceScopeFactory scopes,
    IDateTimeProvider clock,
    ILogger<AchievementsEvaluationJob> logger) : IScheduledJob
{
    public const string JobName = "achievements-evaluation";

    public string Name => JobName;

    public string Description =>
        "Otorga los logros que dependen de un mes cerrado (constancia, presupuesto cumplido y ahorro) y "
        + "recupera los logros instantaneos que no se pudieron otorgar en el momento.";

    public string Schedule => "0 9 * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var instantMode = await jobRuns.HasSucceededAsync(JobName, cancellationToken)
            ? AchievementGrantMode.Recover
            : AchievementGrantMode.Backfill;
        var period = MetricsPeriod.LastClosedMonth(clock.Today);
        var instantGranted = 0;
        var monthlyGranted = 0;
        var failures = new List<string>();

        foreach (var company in await companies.ListActiveAsync(cancellationToken))
        {
            foreach (var employee in await employees.ListByCompanyAsync(company.Id, cancellationToken))
            {
                if (employee.IsWithdrawn)
                {
                    continue;
                }

                instantGranted += await GrantAsync(
                    employee.Id,
                    (granter, ct) => granter.GrantInstantAsync(employee.Id, AchievementCatalog.InstantCodes, instantMode, ct),
                    failures,
                    cancellationToken);
            }

            MonthlyAchievements monthly;

            try
            {
                monthly = await monthlyEvaluator.EvaluateAsync(company.Id, period, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogCompanyFailed(logger, exception, company.Id);
                failures.Add($"Empresa {company.Id}: {exception.GetType().Name}: {exception.Message}");
                continue;
            }

            if (monthly.WithoutExchangeRate.Count > 0)
            {
                failures.Add(string.Create(
                    CultureInfo.InvariantCulture,
                    $"Empresa {company.Id}: sin cotizacion MEP no se evaluo el ahorro de {monthly.WithoutExchangeRate.Count} empleado(s) con movimientos en dolares."));
            }

            foreach (var (employeeId, reached) in monthly.Reached)
            {
                monthlyGranted += await GrantAsync(
                    employeeId,
                    (granter, ct) => granter.GrantReachedAsync(employeeId, reached, AchievementGrantMode.Live, ct),
                    failures,
                    cancellationToken);
            }
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{instantGranted} logro(s) instantaneo(s) {(instantMode == AchievementGrantMode.Backfill ? "otorgados con su historia" : "recuperados")}, "
            + $"{monthlyGranted} de cierre de {period}, {failures.Count} error(es).");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    private async Task<int> GrantAsync(
        Guid employeeId,
        Func<AchievementGranter, CancellationToken, Task<IReadOnlyList<EmployeeAchievement>>> grant,
        List<string> failures,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var granted = await grant(scope.ServiceProvider.GetRequiredService<AchievementGranter>(), cancellationToken);

            return granted.Count;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogEmployeeFailed(logger, exception, employeeId);
            failures.Add($"Empleado {employeeId}: {exception.GetType().Name}: {exception.Message}");

            return 0;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo la evaluacion de los logros de mes cerrado de la empresa {CompanyId}.")]
    private static partial void LogCompanyFailed(ILogger logger, Exception exception, Guid companyId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Fallo el otorgamiento de logros del empleado {EmployeeId}.")]
    private static partial void LogEmployeeFailed(ILogger logger, Exception exception, Guid employeeId);
}
