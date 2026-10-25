namespace FinGrow.Application.UnitTests.Features.Achievements;

using FinGrow.Application.Features.Achievements;
using FinGrow.Application.Features.Achievements.Evaluate;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class AchievementsEvaluationJobTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);

    private readonly CompanyStaffScenario _scenario = new();
    private readonly FakeAchievementRepository _achievements = new();
    private readonly FakeAchievementProgressReadRepository _progress = new();
    private readonly FakeJobRunRepository _jobRuns = new();
    private readonly FakePeriodActivityReadRepository _activity = new();
    private readonly FakeFinancialHealthReadRepository _finances = new();
    private readonly FakeDateTimeProvider _clock = new(Now);
    private readonly ServiceProvider _provider;
    private readonly Employee _employee;

    public AchievementsEvaluationJobTests()
    {
        _employee = _scenario.AddEmployee(department: null);

        var services = new ServiceCollection();
        services.AddSingleton<IAchievementRepository>(_achievements);
        services.AddSingleton<IAchievementProgressReadRepository>(_progress);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IDateTimeProvider>(_clock);
        services.AddScoped<AchievementGranter>();
        _provider = services.BuildServiceProvider();
    }

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task The_first_run_grants_the_history_to_be_celebrated_together()
    {
        _progress.Reach(_employee.Id, AchievementCode.FirstTransaction, Now.AddMonths(-6));
        _progress.Reach(_employee.Id, AchievementCode.FirstGoal, Now.AddMonths(-2));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Summary.ShouldBe("2 logro(s) instantaneo(s) otorgados con su historia, 0 de cierre de 2026-09, 0 error(es).");
        _achievements.Achievements.Count.ShouldBe(2);
        _achievements.Achievements.ShouldAllBe(achievement => !achievement.IsCelebrated);
        _achievements.Achievements.ShouldAllBe(achievement => achievement.DequeueDomainEvents().Count == 0);
    }

    [Fact]
    public async Task Later_runs_recover_failed_grants_without_celebrating_them()
    {
        PreviousSuccessfulRun();
        _progress.Reach(_employee.Id, AchievementCode.FirstBudget, Now.AddDays(-2));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.Summary!.ShouldStartWith("1 logro(s) instantaneo(s) recuperados");
        var recovered = _achievements.Achievements.ShouldHaveSingleItem();
        recovered.AchievedAt.ShouldBe(Now.AddDays(-2));
        recovered.IsCelebrated.ShouldBeTrue();
    }

    [Fact]
    public async Task An_employee_who_withdrew_gets_no_achievement_back_from_what_was_kept()
    {
        PreviousSuccessfulRun();
        _progress.Reach(_employee.Id, AchievementCode.FirstCourse, Now.AddMonths(-1));
        _employee.Withdraw(Now.AddDays(-1));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _achievements.Achievements.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_monthly_achievement_is_announced_to_be_celebrated()
    {
        PreviousSuccessfulRun();
        _activity.Activity[(_scenario.Company.Id, September.Start)] = new()
        {
            new EmployeePeriodActivity(_employee.Id, null, 15, 0, true, false, 0, false),
        };
        _finances.Totals.Add(new EmployeeCurrencyTotals(
            _employee.Id, Currency.ARS, new DateOnly(2026, 1, 5), 100_000m, 50_000m, 100_000m, 50_000m, 50_000m));

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var saver = _achievements.Achievements.ShouldHaveSingleItem();
        saver.Code.ShouldBe(AchievementCode.Saver);
        saver.AchievedAt.ShouldBe(September.EndInstantExclusive);
        saver.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<AchievementUnlocked>();
    }

    [Fact]
    public async Task An_employee_that_fails_is_reported_without_stopping_the_run()
    {
        PreviousSuccessfulRun();
        _progress.Failure = new InvalidOperationException("La base no responde.");

        var result = await Job().ExecuteAsync(CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.ShouldContain(_employee.Id.ToString());
    }

    [Fact]
    public async Task Without_the_mep_rate_the_run_fails_so_the_next_one_measures_savings_again()
    {
        PreviousSuccessfulRun();
        _activity.Activity[(_scenario.Company.Id, September.Start)] = new()
        {
            new EmployeePeriodActivity(_employee.Id, null, 15, 0, false, false, 0, false),
        };
        _finances.Totals.Add(new EmployeeCurrencyTotals(
            _employee.Id, Currency.USD, new DateOnly(2026, 1, 5), 2_000m, 100m, 2_000m, 100m, 100m));

        var result = await Job(new FakeExchangeRateProvider { Failure = new HttpRequestException("DolarApi no responde.") })
            .ExecuteAsync(CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error!.ShouldContain("sin cotizacion MEP");
        _achievements.Achievements.ShouldBeEmpty();
    }

    private void PreviousSuccessfulRun()
    {
        var run = JobRun.Start(AchievementsEvaluationJob.JobName, Now.AddDays(-1));
        run.Succeed(Now.AddDays(-1).AddMinutes(1), null);
        _jobRuns.Add(run);
    }

    private AchievementsEvaluationJob Job(FakeExchangeRateProvider? exchangeRates = null) =>
        new(
            _scenario.Companies,
            _scenario.Employees,
            _jobRuns,
            new MonthlyAchievementsEvaluator(
                _activity,
                _finances,
                exchangeRates ?? new FakeExchangeRateProvider(),
                NullLogger<MonthlyAchievementsEvaluator>.Instance),
            _provider.GetRequiredService<IServiceScopeFactory>(),
            _clock,
            NullLogger<AchievementsEvaluationJob>.Instance);
}
