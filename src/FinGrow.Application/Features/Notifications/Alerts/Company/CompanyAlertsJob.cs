namespace FinGrow.Application.Features.Notifications.Alerts.Company;

using System.Globalization;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal sealed partial class CompanyAlertsJob(
    ICompanyRepository companies,
    IServiceScopeFactory scopes,
    IDateTimeProvider clock,
    ILogger<CompanyAlertsJob> logger) : IScheduledJob
{
    public const string JobName = "company-alerts";

    public string Name => JobName;

    public string Description =>
        "Evalúa las alertas mensuales de bienestar, participación y variación de plantilla según las métricas agregadas.";

    public string Schedule => "0 5 1 * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var notified = 0;
        var failures = new List<string>();

        foreach (var company in await companies.ListActiveAsync(cancellationToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var evaluator = scope.ServiceProvider.GetRequiredService<CompanyAlertsEvaluator>();
                notified += await evaluator.EvaluateAsync(company, clock.UtcNow, cancellationToken);
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

        var summary = string.Create(CultureInfo.InvariantCulture, $"{notified} alerta(s) empresarial(es) generada(s).");
        return failures.Count == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(string.Join(Environment.NewLine, failures), summary);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Falló la evaluación de alertas de la empresa {CompanyId}.")]
    private static partial void LogCompanyFailed(ILogger logger, Exception exception, Guid companyId);
}
