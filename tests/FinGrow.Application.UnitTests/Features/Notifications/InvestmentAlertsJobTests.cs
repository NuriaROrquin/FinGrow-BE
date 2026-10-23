namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Features.Notifications;
using FinGrow.Application.Features.Notifications.Alerts.InvestmentAlerts;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class InvestmentAlertsJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 7);
    private static readonly DateTimeOffset LongAgo = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    private readonly FakeInvestmentRepository _investments = new();
    private readonly FakeNotificationAudienceRepository _audience = new();
    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeNotificationTypeSettingRepository _typeSettings = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly ServiceProvider _provider;

    public InvestmentAlertsJobTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IInvestmentRepository>(_investments);
        services.AddSingleton<INotificationRepository>(_notifications);
        services.AddSingleton<INotificationChannelSettingRepository>(new FakeNotificationChannelSettingRepository());
        services.AddSingleton<INotificationTypeSettingRepository>(_typeSettings);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IDateTimeProvider>(_clock);
        services.AddSingleton<IEnumerable<INotificationSender>>(Array.Empty<INotificationSender>());
        services.AddScoped<Notifier>();

        _provider = services.BuildServiceProvider();
        _audience.EmployeesWithInvestments.Add(EmployeeId);
    }

    [Fact]
    public async Task A_portfolio_that_rises_past_the_threshold_gets_an_alert_with_the_variation()
    {
        QuotedEtf(1000m, (Today.AddDays(-2), 1000m), (Today.AddDays(-1), 1100m));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("1 aviso(s) de variacion del portafolio, 0 de vencimiento, 0 error(es).");
        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Recipient.ShouldBe(NotificationRecipient.Employee(EmployeeId));
        notification.Type.ShouldBe(NotificationType.InvestmentValueChanged);
        notification.Title.ShouldBe(InvestmentAlertsJob.ValueChangedTitle);
        notification.Body.ShouldBe("Tu portafolio en USD subió 10% entre el 05/10 y el 06/10: de USD 1.000 a USD 1.100.");
        notification.DeduplicationKey.ShouldBe("investment-value:USD:2026-10-06");
    }

    [Fact]
    public async Task A_portfolio_that_falls_past_the_threshold_says_it_went_down()
    {
        QuotedEtf(1000m, (Today.AddDays(-2), 1000m), (Today.AddDays(-1), 900m));

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldHaveSingleItem().Body.ShouldContain("bajó 10%");
    }

    [Fact]
    public async Task A_change_below_the_threshold_keeps_the_portfolio_quiet()
    {
        QuotedEtf(1000m, (Today.AddDays(-2), 1000m), (Today.AddDays(-1), 1040m));

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_same_valuation_day_is_only_announced_once()
    {
        QuotedEtf(1000m, (Today.AddDays(-2), 1000m), (Today.AddDays(-1), 1100m));

        await Job().ExecuteAsync(CancellationToken.None);
        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Capital_bought_after_the_previous_valuation_is_not_a_variation()
    {
        var investment = Investment.Create(
            EmployeeId, "SPY", InvestmentType.Etf, Money.From(1000m, Currency.USD), Today.AddDays(-1), LongAgo);
        investment.RecordValuation(Money.From(1200m, Currency.USD), Today.AddDays(-1), ValuationSource.Feed, LongAgo);
        _investments.Add(investment);

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_portfolio_without_recent_valuations_is_not_news()
    {
        QuotedEtf(1000m, (Today.AddDays(-12), 1000m), (Today.AddDays(-11), 1500m));

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_variation_is_measured_per_currency()
    {
        QuotedEtf(1000m, (Today.AddDays(-2), 1000m), (Today.AddDays(-1), 1000m));
        var pesos = Investment.Create(
            EmployeeId, "Fondo", InvestmentType.MutualFund, Money.From(100000m, Currency.ARS), Today.AddDays(-30), LongAgo);
        pesos.RecordValuation(Money.From(100000m, Currency.ARS), Today.AddDays(-2), ValuationSource.Feed, LongAgo);
        pesos.RecordValuation(Money.From(120000m, Currency.ARS), Today.AddDays(-1), ValuationSource.Feed, LongAgo);
        _investments.Add(pesos);

        await Job().ExecuteAsync(CancellationToken.None);

        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Body.ShouldContain("en ARS subió 20%");
    }

    [Fact]
    public async Task A_deposit_a_week_from_maturity_announces_the_date_and_what_it_will_pay()
    {
        Deposit(purchasedOn: Today.AddDays(-23), termDays: 30);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.Summary.ShouldBe("0 aviso(s) de variacion del portafolio, 1 de vencimiento, 0 error(es).");
        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Type.ShouldBe(NotificationType.InvestmentMaturing);
        notification.Title.ShouldBe(InvestmentAlertsJob.MaturingTitle);
        notification.Body.ShouldBe(
            "Tu Plazo fijo Banco Nacion vence en 7 días (14/10/2026). Vas a cobrar ARS 1.024.657,53.");
    }

    [Fact]
    public async Task A_deposit_with_more_than_a_week_left_is_quiet()
    {
        Deposit(purchasedOn: Today.AddDays(-22), termDays: 30);

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_maturity_alert_repeats_only_once_per_milestone()
    {
        Deposit(purchasedOn: Today.AddDays(-23), termDays: 30);

        await Job().ExecuteAsync(CancellationToken.None);
        _clock.UtcNow = Now.AddDays(1);
        await Job().ExecuteAsync(CancellationToken.None);
        _notifications.Notifications.Count.ShouldBe(1);

        _clock.UtcNow = Now.AddDays(6);
        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.Count.ShouldBe(2);
        _notifications.Notifications.Last().Body.ShouldContain("vence mañana");
    }

    [Fact]
    public async Task A_deposit_that_already_matured_is_not_announced()
    {
        Deposit(purchasedOn: Today.AddDays(-31), termDays: 30);

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_remunerated_account_has_no_maturity()
    {
        var account = Investment.Create(
            EmployeeId, "Cuenta remunerada", InvestmentType.RemuneratedAccount, Money.From(100000m, Currency.ARS), Today.AddDays(-5), LongAgo);
        account.AgreeRate(36.5m, null, Today.AddDays(-5), LongAgo);
        _investments.Add(account);

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_employee_who_turned_the_type_off_receives_nothing()
    {
        _typeSettings.Add(NotificationTypeSetting.Create(EmployeeId, NotificationType.InvestmentMaturing, false, Now));
        Deposit(purchasedOn: Today.AddDays(-23), termDays: 30);
        QuotedEtf(1000m, (Today.AddDays(-2), 1000m), (Today.AddDays(-1), 1100m));

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldHaveSingleItem().Type.ShouldBe(NotificationType.InvestmentValueChanged);
    }

    private InvestmentAlertsJob Job() => new(
        _audience,
        _provider.GetRequiredService<IServiceScopeFactory>(),
        _clock,
        NullLogger<InvestmentAlertsJob>.Instance);

    private void QuotedEtf(decimal invested, params (DateOnly ValuedOn, decimal Value)[] valuations)
    {
        var etf = Investment.Create(
            EmployeeId, "SPY", InvestmentType.Etf, Money.From(invested, Currency.USD), Today.AddDays(-30), LongAgo);

        foreach (var (valuedOn, value) in valuations)
        {
            etf.RecordValuation(Money.From(value, Currency.USD), valuedOn, ValuationSource.Feed, LongAgo);
        }

        _investments.Add(etf);
    }

    private void Deposit(DateOnly purchasedOn, int termDays)
    {
        var deposit = Investment.Create(
            EmployeeId, "Plazo fijo Banco Nacion", InvestmentType.FixedTermDeposit, Money.From(1_000_000m, Currency.ARS), purchasedOn, LongAgo);
        deposit.AgreeRate(30m, termDays, purchasedOn, LongAgo);
        _investments.Add(deposit);
    }
}
