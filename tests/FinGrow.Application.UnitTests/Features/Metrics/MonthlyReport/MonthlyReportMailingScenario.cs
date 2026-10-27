namespace FinGrow.Application.UnitTests.Features.Metrics.MonthlyReport;

using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Metrics.GetCompanyInsights;
using FinGrow.Application.Features.Metrics.MonthlyReport;
using FinGrow.Application.Features.Metrics.MonthlyReport.Mailing;
using FinGrow.Application.Features.Metrics.MonthlyReport.Pdf;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// Una empresa con fotos de agosto y septiembre de 2026, el 1 de octubre a las 05:00 de Argentina:
/// el último mes cerrado es septiembre.
/// </summary>
internal sealed class MonthlyReportMailingScenario
{
    public const string PanelUrl = "https://app.fingrow.test/dashboard/company";

    public static readonly DateTimeOffset Now = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset SeptemberCloses = new(2026, 10, 1, 3, 0, 0, TimeSpan.Zero);

    public static readonly MetricsPeriod August = MetricsPeriod.Of(2026, 8);

    public static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);

    public MonthlyReportMailingScenario()
    {
        Company = CompanyStaffScenario.CreateCompany("Acme S.A.", "rrhh@acme.com");
        Companies.Companies.Add(Company);
    }

    public Company Company { get; }

    public FakeCompanyRepository Companies { get; } = new();

    public FakeCompanyNotificationSettingRepository Settings { get; } = new();

    public FakeMonthlyReportDeliveryRepository Deliveries { get; } = new();

    public FakeOutboundEmailRepository OutboundEmails { get; } = new();

    public FakeMetricsSnapshotRepository Snapshots { get; } = new();

    public FakeFinancialHealthScoreRepository Scores { get; } = new();

    public FakeCompanyEducationMetricsReadRepository Education { get; } = new();

    public FakeUnitOfWork UnitOfWork { get; } = new();

    public FakeAiService Ai { get; } = new();

    public FakeCompanyReportPdfRenderer Renderer { get; } = new();

    public static MetricsAnonymizationPolicy Policy => new(Options.Create(new MetricsAnonymizationOptions()));

    public static IOptions<CompanyMonthlyReportOptions> MailOptions =>
        Options.Create(new CompanyMonthlyReportOptions { PanelUrl = PanelUrl });

    public CompanyMonthlyReportBuilder Builder => new(Snapshots, Scores, Education, Policy);

    public CompanyInsightsBuilder InsightsBuilder =>
        new(Snapshots, Scores, Policy, Ai, NullLogger<CompanyInsightsBuilder>.Instance);

    public CompanyReportPdfBuilder PdfBuilder => new(Builder, InsightsBuilder, Snapshots, Policy, Renderer);

    public CompanyMonthlyReportMailer Mailer(ICompanyNotificationSettingRepository? settings = null) =>
        new(settings ?? Settings, Deliveries, OutboundEmails, PdfBuilder, MailOptions, UnitOfWork);

    public CompanyNotificationSetting Subscribe(DateTimeOffset enabledAt, Company? company = null)
    {
        var setting = CompanyNotificationSetting.Create(
            (company ?? Company).Id, NotificationType.MonthlyReport, isEnabled: true, NotificationCadence.Monthly, enabledAt);
        Settings.Add(setting);
        return setting;
    }

    /// <summary>Los mismos números que el reporte de HU-65: participación 50 → 80, puntaje 60 → 70, cursos 2 → 6.</summary>
    public List<Guid> PhotographAugustAndSeptember(Company? company = null)
    {
        var owner = company ?? Company;
        TakeSnapshot(owner, August, PeriodMetrics.From(10, 5, 100, 2, 4, 1, 3, 4, 240));
        var employees = AddScores(August, LowSavings(), LowSavings(), LowSavings(), Healthy());
        Education.CompletedCourses[(owner.Id, August.Start)] = 2;
        TakeSnapshot(owner, September, PeriodMetrics.From(10, 8, 120, 2, 3, 4, 3, 5, 350));
        employees.AddRange(AddScores(September, LowSavings(), LowSavings(), LowSavings(), Healthy(), Healthy()));
        Education.CompletedCourses[(owner.Id, September.Start)] = 6;
        return employees;
    }

    public void TakeSnapshot(Company company, MetricsPeriod period, PeriodMetrics metrics) =>
        Snapshots.Add(CompanyMetricsSnapshot.Take(company.Id, period, metrics, Now));

    public void TakeSnapshot(MetricsPeriod period, PeriodMetrics metrics) => TakeSnapshot(Company, period, metrics);

    public async Task<CompanyMonthlyReportResponse> SeptemberReportAsync() =>
        (await Builder.BuildAsync(Company, September, CancellationToken.None)).ShouldNotBeNull();

    private List<Guid> AddScores(MetricsPeriod period, params FinancialHealthFactors[] factors)
    {
        var employeeIds = new List<Guid>();

        foreach (var factor in factors)
        {
            var employeeId = Guid.CreateVersion7();
            employeeIds.Add(employeeId);
            Scores.Add(FinancialHealthScore.Compute(employeeId, period, factor, Now));
        }

        return employeeIds;
    }

    private static FinancialHealthFactors Healthy() => FinancialHealthFactors.From(100m, null, null, 100m);

    private static FinancialHealthFactors LowSavings() => FinancialHealthFactors.From(20m, null, null, 100m);
}
