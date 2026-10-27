namespace FinGrow.Application.Features.Metrics.MonthlyReport.Mailing;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// HU-82: manda por mail el reporte del último mes cerrado a las empresas que prendieron "Reportes
/// Mensuales". Corre todos los días y no solo el 1 porque la foto del mes puede llegar tarde (sin MEP,
/// <c>metrics-snapshot</c> la deja para la corrida siguiente); <c>monthly_report_deliveries</c> es lo
/// que evita mandar el mismo mes dos veces.
/// </summary>
internal sealed partial class CompanyMonthlyReportJob(
    ICompanyRepository companies,
    IServiceScopeFactory scopes,
    IDateTimeProvider clock,
    ILogger<CompanyMonthlyReportJob> logger) : IScheduledJob
{
    public const string JobName = "company-monthly-report";

    public string Name => JobName;

    public string Description =>
        "Manda por mail el reporte del último mes cerrado a las empresas que activaron Reportes Mensuales, una sola vez por mes.";

    public string Schedule => "0 8 * * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var queued = 0;
        var waiting = 0;
        var failures = new List<string>();

        foreach (var company in await companies.ListActiveAsync(cancellationToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var mailer = scope.ServiceProvider.GetRequiredService<CompanyMonthlyReportMailer>();

                switch (await mailer.SendAsync(company, now, cancellationToken))
                {
                    case MonthlyReportMailOutcome.Queued:
                    case MonthlyReportMailOutcome.Requeued:
                        queued++;
                        break;
                    case MonthlyReportMailOutcome.WaitingForSnapshot:
                        waiting++;
                        break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogCompanyFailed(logger, exception, company.Id);
                failures.Add($"{company.Id}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        var summary = string.Create(
            CultureInfo.InvariantCulture,
            $"{queued} reporte(s) mensual(es) encolado(s), {waiting} esperando la foto del mes, {failures.Count} error(es).");

        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Falló el reporte mensual de la empresa {CompanyId}.")]
    private static partial void LogCompanyFailed(ILogger logger, Exception exception, Guid companyId);
}
