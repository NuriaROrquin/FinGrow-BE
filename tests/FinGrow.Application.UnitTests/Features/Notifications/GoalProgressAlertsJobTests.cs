namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Features.Notifications;
using FinGrow.Application.Features.Notifications.Alerts.GoalProgress;
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

public class GoalProgressAlertsJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 7);
    private static readonly DateTimeOffset LongAgo = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly string[] MilestonesInOrder = { "30", "7", "1" };

    private readonly FakeGoalRepository _goals = new();
    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly ServiceProvider _provider;

    public GoalProgressAlertsJobTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IGoalRepository>(_goals);
        services.AddSingleton<INotificationRepository>(_notifications);
        services.AddSingleton<INotificationChannelSettingRepository>(new FakeNotificationChannelSettingRepository());
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IDateTimeProvider>(_clock);
        services.AddSingleton<IEnumerable<INotificationSender>>(Array.Empty<INotificationSender>());
        services.AddScoped<Notifier>();

        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task A_goal_without_contributions_for_the_whole_period_gets_a_reminder()
    {
        var goal = NewGoal("Viaje", 100000m, Today.AddDays(120));
        goal.AddContribution(Money.From(25000m, Currency.ARS), Today.AddDays(-GoalProgressAlertsJob.StalledAfterDays), null, LongAgo);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("1 aviso(s) de meta sin aportes, 0 de fecha limite cercana, 0 error(es).");
        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Recipient.ShouldBe(NotificationRecipient.Employee(EmployeeId));
        notification.Type.ShouldBe(NotificationType.GoalStalled);
        notification.Title.ShouldBe(GoalProgressAlertsJob.StalledTitle);
        notification.Body.ShouldBe(
            "Hace 30 días que no sumás nada a \"Viaje\". Llevás el 25% y te faltan ARS 75.000: un aporte chico ya te acerca.");
    }

    [Fact]
    public async Task A_recent_contribution_keeps_the_goal_quiet()
    {
        var goal = NewGoal("Viaje", 100000m, Today.AddDays(120));
        goal.AddContribution(Money.From(25000m, Currency.ARS), Today.AddDays(-GoalProgressAlertsJob.StalledAfterDays + 1), null, LongAgo);

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_goal_created_recently_without_contributions_is_not_stalled_yet()
    {
        NewGoal("Viaje", 100000m, Today.AddDays(120), createdAt: Now.AddDays(-10));

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_stalled_reminder_repeats_once_per_period_while_the_goal_stays_still()
    {
        NewGoal("Viaje", 100000m, Today.AddDays(200));

        await Job().ExecuteAsync(CancellationToken.None);
        _clock.UtcNow = Now.AddDays(1);
        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.Count.ShouldBe(1);

        _clock.UtcNow = Now.AddDays(GoalProgressAlertsJob.StalledAfterDays);
        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.Count.ShouldBe(2);
    }

    [Fact]
    public async Task An_approaching_deadline_says_how_much_is_missing_and_how_many_days_are_left()
    {
        var goal = NewGoal("Notebook", 800000m, Today.AddDays(7));
        goal.AddContribution(Money.From(569999.5m, Currency.ARS), Today.AddDays(-2), null, Now.AddDays(-2));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.Summary.ShouldBe("0 aviso(s) de meta sin aportes, 1 de fecha limite cercana, 0 error(es).");
        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Type.ShouldBe(NotificationType.GoalDeadlineApproaching);
        notification.Title.ShouldBe(GoalProgressAlertsJob.DeadlineTitle);
        notification.Body.ShouldBe("Te faltan ARS 230.000,5 para completar \"Notebook\" y quedan 7 días (vence el 14/10/2026).");
    }

    [Theory]
    [InlineData(1, "Te faltan USD 1.000 para completar \"Auto\" y queda 1 día (vence el 08/10/2026).")]
    [InlineData(0, "Te faltan USD 1.000 para completar \"Auto\" y vence hoy.")]
    public async Task The_last_days_are_worded_for_what_they_are(int daysRemaining, string body)
    {
        NewGoal("Auto", 1000m, Today.AddDays(daysRemaining), currency: Currency.USD);

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldHaveSingleItem().Body.ShouldBe(body);
    }

    [Fact]
    public async Task Each_deadline_milestone_alerts_only_once()
    {
        NewGoal("Notebook", 800000m, Today.AddDays(8), createdAt: Now.AddDays(-5));

        for (var day = 0; day <= 8; day++)
        {
            _clock.UtcNow = Now.AddDays(day);
            await Job().ExecuteAsync(CancellationToken.None);
        }

        _notifications.Notifications
            .Select(notification => notification.DeduplicationKey.Split(':').Last())
            .ShouldBe(MilestonesInOrder);
    }

    [Fact]
    public async Task Moving_the_deadline_restarts_its_milestones()
    {
        var goal = NewGoal("Notebook", 800000m, Today.AddDays(5), createdAt: Now.AddDays(-5));
        await Job().ExecuteAsync(CancellationToken.None);

        goal.UpdateDetails(goal.Name, goal.TargetAmount, Today.AddDays(6), Now);
        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_stalled_goal_close_to_its_deadline_gets_only_the_deadline_alert()
    {
        NewGoal("Viaje", 100000m, Today.AddDays(3));

        await Job().ExecuteAsync(CancellationToken.None);

        _notifications.Notifications.ShouldHaveSingleItem().Type.ShouldBe(NotificationType.GoalDeadlineApproaching);
    }

    [Fact]
    public async Task Achieved_cancelled_and_overdue_goals_do_not_alert()
    {
        var achieved = NewGoal("Lograda", 1000m, Today.AddDays(3));
        achieved.AddContribution(Money.From(1000m, Currency.ARS), Today, null, Now);
        NewGoal("Cancelada", 1000m, Today.AddDays(3)).Cancel(Now);
        NewGoal("Vencida", 1000m, Today.AddDays(3));
        _clock.UtcNow = Now.AddDays(4);

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _notifications.Notifications.ShouldBeEmpty();
    }

    private Goal NewGoal(
        string name,
        decimal target,
        DateOnly deadline,
        DateTimeOffset? createdAt = null,
        Currency currency = Currency.ARS)
    {
        var goal = Goal.Create(EmployeeId, name, Money.From(target, currency), deadline, createdAt ?? LongAgo);
        _goals.Add(goal);

        return goal;
    }

    private GoalProgressAlertsJob Job() => new(
        _goals,
        _provider.GetRequiredService<IServiceScopeFactory>(),
        _clock,
        NullLogger<GoalProgressAlertsJob>.Instance);
}
