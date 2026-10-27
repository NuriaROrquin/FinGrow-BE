namespace FinGrow.Application.UnitTests.Features.Metrics.MonthlyReport;

using FinGrow.Application.Features.Metrics.MonthlyReport.Mailing;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using static MonthlyReportMailingScenario;

public class CompanyMonthlyReportJobTests
{
    private readonly MonthlyReportMailingScenario _scenario = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly Company _other = CompanyStaffScenario.CreateCompany("Otra S.A.", "rrhh@otra.com");

    public CompanyMonthlyReportJobTests() => _scenario.Companies.Companies.Add(_other);

    [Fact]
    public async Task Only_subscribed_companies_get_the_report()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.PhotographAugustAndSeptember();
        _scenario.PhotographAugustAndSeptember(_other);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("1 reporte(s) mensual(es) encolado(s), 0 esperando la foto del mes, 0 error(es).");
        _scenario.OutboundEmails.Emails.ShouldHaveSingleItem().Recipient.Value.ShouldBe("rrhh@acme.com");
    }

    [Fact]
    public async Task Running_every_day_sends_each_month_once()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.PhotographAugustAndSeptember();

        await Job().ExecuteAsync(CancellationToken.None);
        _clock.UtcNow = Now.AddDays(1);
        var second = await Job().ExecuteAsync(CancellationToken.None);

        second.Summary.ShouldBe("0 reporte(s) mensual(es) encolado(s), 0 esperando la foto del mes, 0 error(es).");
        _scenario.OutboundEmails.Emails.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_month_without_photo_waits_and_goes_out_the_day_the_photo_arrives()
    {
        _scenario.Subscribe(Now.AddMonths(-2));

        var first = await Job().ExecuteAsync(CancellationToken.None);
        _scenario.PhotographAugustAndSeptember();
        _clock.UtcNow = Now.AddDays(1);
        var second = await Job().ExecuteAsync(CancellationToken.None);

        first.IsSuccess.ShouldBeTrue();
        first.Summary.ShouldBe("0 reporte(s) mensual(es) encolado(s), 1 esperando la foto del mes, 0 error(es).");
        second.Summary.ShouldBe("1 reporte(s) mensual(es) encolado(s), 0 esperando la foto del mes, 0 error(es).");
        _scenario.OutboundEmails.Emails.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_company_that_fails_is_recorded_and_does_not_stop_the_others()
    {
        _scenario.Subscribe(Now.AddMonths(-2));
        _scenario.Subscribe(Now.AddMonths(-2), _other);
        _scenario.PhotographAugustAndSeptember();
        _scenario.PhotographAugustAndSeptember(_other);

        var result = await Job(new FailingSettingsFor(_scenario.Settings, _other.Id)).ExecuteAsync(CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull().ShouldContain(_other.Id.ToString());
        result.Summary.ShouldBe("1 reporte(s) mensual(es) encolado(s), 0 esperando la foto del mes, 1 error(es).");
        _scenario.OutboundEmails.Emails.ShouldHaveSingleItem().Recipient.Value.ShouldBe("rrhh@acme.com");
        _scenario.Deliveries.Deliveries.ShouldNotContain(delivery => delivery.CompanyId == _other.Id);
    }

    [Fact]
    public void The_job_is_named_and_scheduled_to_run_every_day()
    {
        var job = Job();

        job.Name.ShouldBe("company-monthly-report");
        job.Schedule.ShouldBe("0 8 * * *");
    }

    private CompanyMonthlyReportJob Job(ICompanyNotificationSettingRepository? settings = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(settings ?? _scenario.Settings);
        services.AddSingleton<IMonthlyReportDeliveryRepository>(_scenario.Deliveries);
        services.AddSingleton<IOutboundEmailRepository>(_scenario.OutboundEmails);
        services.AddSingleton<IUnitOfWork>(_scenario.UnitOfWork);
        services.AddSingleton(MailOptions);
        services.AddScoped(_ => _scenario.PdfBuilder);
        services.AddScoped<CompanyMonthlyReportMailer>();

        return new CompanyMonthlyReportJob(
            _scenario.Companies,
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            _clock,
            NullLogger<CompanyMonthlyReportJob>.Instance);
    }

    private sealed class FailingSettingsFor(ICompanyNotificationSettingRepository inner, Guid failingCompanyId)
        : ICompanyNotificationSettingRepository
    {
        public void Add(CompanyNotificationSetting setting) => inner.Add(setting);

        public Task<IReadOnlyList<CompanyNotificationSetting>> ListByCompanyAsync(
            Guid companyId,
            CancellationToken cancellationToken = default) =>
            companyId == failingCompanyId
                ? throw new InvalidOperationException("La base no respondió.")
                : inner.ListByCompanyAsync(companyId, cancellationToken);
    }
}
