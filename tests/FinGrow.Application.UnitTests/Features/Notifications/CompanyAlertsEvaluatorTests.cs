namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Features.Metrics;
using FinGrow.Application.Features.Notifications;
using FinGrow.Application.Features.Notifications.Alerts.Company;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public class CompanyAlertsEvaluatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeCompanyNotificationSettingRepository _settings = new();
    private readonly FakeMetricsSnapshotRepository _snapshots = new();
    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly Company _company = Company.Create(
        "Acme S.A.",
        TaxId.From("20123456786"),
        Email.From("rrhh@acme.com"),
        "hash",
        Currency.ARS,
        Now.AddMonths(-6));

    [Fact]
    public async Task Workforce_growth_is_calculated_from_consecutive_monthly_snapshots()
    {
        var setting = CompanyNotificationSetting.Create(
            _company.Id, NotificationType.EmployeeAdded, isEnabled: true, NotificationCadence.Monthly, Now.AddMonths(-2));
        _settings.Add(setting);
        _snapshots.Add(Snapshot(new DateOnly(2026, 8, 1), activeEmployees: 10, scoreSum: 650));
        _snapshots.Add(Snapshot(new DateOnly(2026, 9, 1), activeEmployees: 13, scoreSum: 845));

        var notifications = await CreateEvaluator().EvaluateAsync(_company, Now, CancellationToken.None);

        notifications.ShouldBe(1);
        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.RecipientType.ShouldBe(NotificationRecipientType.Company);
        notification.RecipientId.ShouldBe(_company.Id);
        notification.Type.ShouldBe(NotificationType.EmployeeAdded);
        notification.Title.ShouldBe("Aumento de plantilla");
        notification.Body.ShouldContain("aumentó en 3 empleado(s)");
        notification.Body.ShouldNotContain("Ana");
        setting.LastEvaluatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Workforce_reduction_is_calculated_from_consecutive_monthly_snapshots()
    {
        var setting = CompanyNotificationSetting.Create(
            _company.Id, NotificationType.EmployeeRemoved, isEnabled: true, NotificationCadence.Monthly, Now.AddMonths(-2));
        _settings.Add(setting);
        _snapshots.Add(Snapshot(new DateOnly(2026, 8, 1), activeEmployees: 13, scoreSum: 845));
        _snapshots.Add(Snapshot(new DateOnly(2026, 9, 1), activeEmployees: 10, scoreSum: 650));

        var notifications = await CreateEvaluator().EvaluateAsync(_company, Now, CancellationToken.None);

        notifications.ShouldBe(1);
        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Type.ShouldBe(NotificationType.EmployeeRemoved);
        notification.Title.ShouldBe("Reducción de plantilla");
        notification.Body.ShouldContain("disminuyó en 3 empleado(s)");
    }

    [Fact]
    public async Task Workforce_change_requires_a_previous_snapshot()
    {
        _settings.Add(CompanyNotificationSetting.Create(
            _company.Id, NotificationType.EmployeeAdded, isEnabled: true, NotificationCadence.Monthly, Now.AddMonths(-2)));
        _snapshots.Add(Snapshot(new DateOnly(2026, 9, 1), activeEmployees: 13, scoreSum: 845));

        var notifications = await CreateEvaluator().EvaluateAsync(_company, Now, CancellationToken.None);

        notifications.ShouldBe(0);
        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unchanged_workforce_does_not_emit_an_alert()
    {
        _settings.Add(CompanyNotificationSetting.Create(
            _company.Id, NotificationType.EmployeeAdded, isEnabled: true, NotificationCadence.Monthly, Now.AddMonths(-2)));
        _snapshots.Add(Snapshot(new DateOnly(2026, 8, 1), activeEmployees: 10, scoreSum: 650));
        _snapshots.Add(Snapshot(new DateOnly(2026, 9, 1), activeEmployees: 10, scoreSum: 650));

        var notifications = await CreateEvaluator().EvaluateAsync(_company, Now, CancellationToken.None);

        notifications.ShouldBe(0);
        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task Low_financial_wellbeing_is_not_sent_below_the_anonymity_threshold()
    {
        _settings.Add(CompanyNotificationSetting.Create(
            _company.Id, NotificationType.FinancialWellbeingLow, isEnabled: true, NotificationCadence.Monthly, Now.AddMonths(-1)));
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _company.Id,
            MetricsPeriod.MonthOf(new DateOnly(2026, 9, 1)),
            PeriodMetrics.From(2, 1, 20, 1, 1, 0, 0, 2, 80),
            Now.AddDays(-1)));

        var notifications = await CreateEvaluator().EvaluateAsync(_company, Now, CancellationToken.None);

        notifications.ShouldBe(0);
        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task Low_financial_wellbeing_is_not_sent_with_less_than_three_employees_with_score()
    {
        _settings.Add(CompanyNotificationSetting.Create(
            _company.Id, NotificationType.FinancialWellbeingLow, isEnabled: true, NotificationCadence.Monthly, Now.AddMonths(-1)));
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _company.Id,
            MetricsPeriod.MonthOf(new DateOnly(2026, 9, 1)),
            PeriodMetrics.From(10, 4, 60, 3, 3, 0, 2, 2, 60),
            Now.AddDays(-1)));

        var notifications = await CreateEvaluator().EvaluateAsync(_company, Now, CancellationToken.None);

        notifications.ShouldBe(0);
        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_custom_wellbeing_threshold_is_used_for_the_alert()
    {
        _settings.Add(CompanyNotificationSetting.Create(
            _company.Id,
            NotificationType.FinancialWellbeingLow,
            isEnabled: true,
            NotificationCadence.Monthly,
            Now.AddMonths(-1),
            threshold: 70m));
        _snapshots.Add(CompanyMetricsSnapshot.Take(
            _company.Id,
            MetricsPeriod.MonthOf(new DateOnly(2026, 9, 1)),
            PeriodMetrics.From(3, 2, 30, 2, 2, 1, 1, 3, 195),
            Now.AddDays(-1)));

        var notifications = await CreateEvaluator().EvaluateAsync(_company, Now, CancellationToken.None);

        notifications.ShouldBe(1);
        _notifications.Notifications.ShouldHaveSingleItem().Type.ShouldBe(NotificationType.FinancialWellbeingLow);
    }

    private CompanyAlertsEvaluator CreateEvaluator() =>
        new(
            _settings,
            _snapshots,
            new MetricsAnonymizationPolicy(Options.Create(new MetricsAnonymizationOptions())),
            new Notifier(
                _notifications,
                new FakeNotificationChannelSettingRepository(),
                new FakeNotificationTypeSettingRepository(),
                _settings,
                Array.Empty<INotificationSender>(),
                _unitOfWork,
                _clock,
                NullLogger<Notifier>.Instance),
            _unitOfWork);

    private CompanyMetricsSnapshot Snapshot(DateOnly periodStart, int activeEmployees, int scoreSum) =>
        CompanyMetricsSnapshot.Take(
            _company.Id,
            MetricsPeriod.MonthOf(periodStart),
            PeriodMetrics.From(activeEmployees, 2, 30, 2, 2, 1, 1, activeEmployees, scoreSum),
            Now.AddDays(-1));
}
