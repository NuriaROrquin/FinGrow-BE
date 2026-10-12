namespace FinGrow.Application.Features.Metrics.SnapshotMetrics;

using System.Globalization;
using FinGrow.Application.Features.Metrics.FinancialHealth;
using FinGrow.Application.Interfaces;
using FinGrow.Application.Jobs;
using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

internal sealed partial class MetricsSnapshotJob : IScheduledJob
{
    public const string JobName = "metrics-snapshot";

    private readonly ICompanyRepository _companies;
    private readonly IPeriodActivityReadRepository _activity;
    private readonly IMetricsSnapshotRepository _snapshots;
    private readonly FinancialHealthScorer _healthScorer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<MetricsSnapshotJob> _logger;

    public MetricsSnapshotJob(
        ICompanyRepository companies,
        IPeriodActivityReadRepository activity,
        IMetricsSnapshotRepository snapshots,
        FinancialHealthScorer healthScorer,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        ILogger<MetricsSnapshotJob> logger)
    {
        _companies = companies;
        _activity = activity;
        _snapshots = snapshots;
        _healthScorer = healthScorer;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public string Name => JobName;

    public string Description =>
        "Genera las fotos mensuales de metricas de cada empresa y de sus departamentos, con el puntaje de "
        + "salud financiera de cada empleado: siempre la del ultimo mes cerrado y ademas las de los meses "
        + "anteriores que todavia no tengan foto.";

    public string Schedule => "0 4 1 * *";

    public async Task<JobResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var lastClosed = MetricsPeriod.LastClosedMonth(ArgentinaTime.DateOf(now));
        var companies = await _companies.ListActiveAsync(cancellationToken);
        var companySnapshots = 0;
        var departmentSnapshots = 0;
        var periodsWithoutExchangeRate = 0;
        var periods = new HashSet<MetricsPeriod>();

        foreach (var company in companies)
        {
            var alreadyTaken = await _snapshots.ListCompanyPeriodStartsAsync(company.Id, cancellationToken);
            var firstPeriod = MetricsPeriod.MonthOf(ArgentinaTime.DateOf(company.CreatedAt));
            var pending = firstPeriod
                .Until(lastClosed)
                .Where(period => period == lastClosed || !alreadyTaken.Contains(period.Start))
                .ToList();

            foreach (var period in pending)
            {
                var activity = await _activity.ListByCompanyAsync(company.Id, period, cancellationToken);
                var scoring = await _healthScorer.ScoreAsync(company.Id, period, activity, now, cancellationToken);

                if (!scoring.IsComplete)
                {
                    LogPeriodWithoutExchangeRate(_logger, company.Name, period.ToString());
                    periodsWithoutExchangeRate++;
                    continue;
                }

                await SnapshotCompanyAsync(company, period, activity, scoring.Scores, now, cancellationToken);
                companySnapshots++;

                foreach (var department in company.Departments.Where(department => ExistedDuring(department, period)))
                {
                    var ownActivity = activity.Where(employee => employee.DepartmentId == department.Id);

                    await SnapshotDepartmentAsync(department, period, ownActivity, scoring.Scores, now, cancellationToken);
                    departmentSnapshots++;
                }

                periods.Add(period);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            LogCompanyDone(_logger, company.Name, pending.Count);
        }

        var summary = Summarize(companies.Count, companySnapshots, departmentSnapshots, periods);

        return periodsWithoutExchangeRate == 0
            ? JobResult.Success(summary)
            : JobResult.Failure(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"No se pudo obtener la cotizacion del dolar MEP: {periodsWithoutExchangeRate} foto(s) de empresa quedaron pendientes para la proxima corrida."),
                summary);
    }

    private static bool ExistedDuring(Department department, MetricsPeriod period) =>
        department.WasActiveDuring(period.StartInstant, period.EndInstantExclusive);

    private async Task SnapshotCompanyAsync(
        Company company,
        MetricsPeriod period,
        IEnumerable<EmployeePeriodActivity> activity,
        IReadOnlyDictionary<Guid, int> scores,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var metrics = PeriodMetricsCalculator.Aggregate(activity, scores);
        var existing = await _snapshots.FindCompanyAsync(company.Id, period.Start, cancellationToken);

        if (existing is null)
        {
            _snapshots.Add(CompanyMetricsSnapshot.Take(company.Id, period, metrics, now));
        }
        else
        {
            existing.Refresh(metrics, now);
        }
    }

    private async Task SnapshotDepartmentAsync(
        Department department,
        MetricsPeriod period,
        IEnumerable<EmployeePeriodActivity> activity,
        IReadOnlyDictionary<Guid, int> scores,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var metrics = PeriodMetricsCalculator.Aggregate(activity, scores);
        var existing = await _snapshots.FindDepartmentAsync(department.Id, period.Start, cancellationToken);

        if (existing is null)
        {
            _snapshots.Add(DepartmentMetricsSnapshot.Take(department.CompanyId, department.Id, period, metrics, now));
        }
        else
        {
            existing.Refresh(metrics, now);
        }
    }

    private static string Summarize(int companies, int companySnapshots, int departmentSnapshots, HashSet<MetricsPeriod> periods)
    {
        if (periods.Count == 0)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{companies} empresa(s) activa(s); ninguna tenia un mes cerrado para fotografiar.");
        }

        var first = periods.Min(period => period.Start);
        var last = periods.Max(period => period.Start);
        var range = first == last
            ? MetricsPeriod.FromStart(first).ToString()
            : $"{MetricsPeriod.FromStart(first)} a {MetricsPeriod.FromStart(last)}";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{companies} empresa(s) activa(s): {companySnapshots} foto(s) de empresa y {departmentSnapshots} de departamento para {periods.Count} periodo(s) ({range}).");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fotos de metricas de {Company}: {Periods} periodo(s) generados o actualizados.")]
    private static partial void LogCompanyDone(ILogger logger, string company, int periods);

    [LoggerMessage(Level = LogLevel.Warning, Message = "La foto de {Company} de {Period} queda pendiente: sin cotizacion MEP no se puede medir la salud financiera.")]
    private static partial void LogPeriodWithoutExchangeRate(ILogger logger, string company, string period);
}
