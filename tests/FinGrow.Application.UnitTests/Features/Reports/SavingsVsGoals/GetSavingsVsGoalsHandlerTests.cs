namespace FinGrow.Application.UnitTests.Features.Reports.SavingsVsGoals;

using Common;
using FinGrow.Application.Features.Reports.SavingsVsGoals;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class GetSavingsVsGoalsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MidMay = new(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeGoalRepository _goals = new();
    private readonly GetSavingsVsGoalsHandler _handler;

    public GetSavingsVsGoalsHandlerTests() =>
        _handler = new GetSavingsVsGoalsHandler(_goals, new FakeDateTimeProvider(Now));

    private Task<Result<SavingsVsGoalsResponse>> RunAsync(int? months = 6, Currency currency = Currency.ARS) =>
        _handler.Handle(new GetSavingsVsGoalsQuery(_employeeId, currency, months), CancellationToken.None);

    private Goal StoreGoal(decimal target, DateOnly deadline, Currency currency = Currency.ARS)
    {
        var goal = Goal.Create(_employeeId, "Meta", Money.From(target, currency), deadline, MidMay);
        _goals.Add(goal);
        return goal;
    }

    /// <summary>Registra el aporte el mismo dia de su fecha, como si se cargara en el momento.</summary>
    private static void Contribute(Goal goal, decimal amount, DateOnly on) =>
        goal.AddContribution(
            Money.From(amount, goal.TargetAmount.Currency),
            on,
            null,
            new DateTimeOffset(on, new TimeOnly(15, 0), TimeSpan.Zero));

    [Fact]
    public async Task The_series_covers_the_requested_months_up_to_the_current_one()
    {
        var report = (await RunAsync(months: 6)).Value;

        report.Months.Select(month => (month.Year, month.Month)).ShouldBe(new[]
        {
            (2026, 4), (2026, 5), (2026, 6), (2026, 7), (2026, 8), (2026, 9)
        });
    }

    [Fact]
    public async Task Each_month_compares_what_was_contributed_against_the_monthly_commitment()
    {
        // De mayo a agosto: 4 meses de plan, 1000 por mes.
        var goal = StoreGoal(4000m, new DateOnly(2026, 8, 31));
        Contribute(goal, 1200m, new DateOnly(2026, 5, 20));
        Contribute(goal, 500m, new DateOnly(2026, 6, 3));

        var report = (await RunAsync(months: 6)).Value;

        var april = report.Months[0];
        april.Committed.ShouldBe(0m);
        april.MetTarget.ShouldBeNull();

        report.Months[1].ShouldSatisfyAllConditions(
            may => may.Committed.ShouldBe(1000m),
            may => may.Contributed.ShouldBe(1200m),
            may => may.MetTarget.ShouldBe(true));

        report.Months[2].ShouldSatisfyAllConditions(
            june => june.Contributed.ShouldBe(500m),
            june => june.MetTarget.ShouldBe(false));

        report.MonthsWithCommitment.ShouldBe(4);
        report.MonthsOnTarget.ShouldBe(1);
        report.TotalCommitted.ShouldBe(4000m);
        report.TotalContributed.ShouldBe(1700m);
        report.CompletionRate.ShouldBe(42.5m);
    }

    [Fact]
    public async Task The_current_month_is_not_counted_as_missed_before_it_ends()
    {
        // De mayo a diciembre: 8 meses, 500 por mes. Septiembre es el mes en curso.
        var goal = StoreGoal(4000m, new DateOnly(2026, 12, 31));
        Contribute(goal, 300m, new DateOnly(2026, 9, 5));

        var report = (await RunAsync(months: 1)).Value;

        var september = report.Months.ShouldHaveSingleItem();
        september.Committed.ShouldBe(500m);
        september.MetTarget.ShouldBeNull();
        report.MonthsWithCommitment.ShouldBe(0);
        report.MonthsOnTarget.ShouldBe(0);
    }

    [Fact]
    public async Task The_current_month_counts_as_soon_as_its_commitment_is_reached()
    {
        var goal = StoreGoal(4000m, new DateOnly(2026, 12, 31));
        Contribute(goal, 500m, new DateOnly(2026, 9, 5));

        var report = (await RunAsync(months: 1)).Value;

        report.Months[0].MetTarget.ShouldBe(true);
        report.MonthsWithCommitment.ShouldBe(1);
        report.MonthsOnTarget.ShouldBe(1);
    }

    [Fact]
    public async Task The_commitments_and_the_contributions_of_every_goal_add_up()
    {
        var holidays = StoreGoal(4000m, new DateOnly(2026, 8, 31));
        var laptop = StoreGoal(2000m, new DateOnly(2026, 6, 30));
        Contribute(holidays, 300m, new DateOnly(2026, 6, 1));
        Contribute(laptop, 900m, new DateOnly(2026, 6, 15));

        var june = (await RunAsync(months: 6)).Value.Months[2];

        june.Committed.ShouldBe(1000m + 1000m);
        june.Contributed.ShouldBe(1200m);
    }

    [Fact]
    public async Task An_achieved_goal_stops_committing_after_the_month_it_was_reached()
    {
        var goal = StoreGoal(4000m, new DateOnly(2026, 8, 31));
        Contribute(goal, 4000m, new DateOnly(2026, 7, 5));

        var report = (await RunAsync(months: 6)).Value;

        report.Months[3].ShouldSatisfyAllConditions(
            july => july.Committed.ShouldBe(1000m),
            july => july.Contributed.ShouldBe(4000m));
        report.Months[4].Committed.ShouldBe(0m);
    }

    [Fact]
    public async Task A_goal_completed_later_with_a_backdated_contribution_stops_appearing_after_that_contribution_month()
    {
        var goal = StoreGoal(4000m, new DateOnly(2026, 8, 31));
        // Se carga en septiembre, pero con fecha de junio: la meta se completo en junio.
        goal.AddContribution(
            Money.From(4000m, Currency.ARS),
            new DateOnly(2026, 6, 10),
            null,
            new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero));

        var report = (await RunAsync(months: 6)).Value;

        report.Months[2].Committed.ShouldBe(1000m);
        report.Months[3].Committed.ShouldBe(0m);
        report.Months[4].Committed.ShouldBe(0m);
    }

    [Fact]
    public async Task Goals_and_contributions_in_another_currency_are_left_out()
    {
        var goal = StoreGoal(4000m, new DateOnly(2026, 8, 31), Currency.USD);
        Contribute(goal, 500m, new DateOnly(2026, 6, 1));

        var report = (await RunAsync(months: 6, currency: Currency.ARS)).Value;

        report.HasGoals.ShouldBeFalse();
        report.TotalCommitted.ShouldBe(0m);
        report.TotalContributed.ShouldBe(0m);
    }

    [Fact]
    public async Task Without_anything_committed_there_is_no_completion_rate()
    {
        var report = (await RunAsync(months: 6)).Value;

        report.CompletionRate.ShouldBeNull();
    }

    [Fact]
    public async Task Without_a_period_the_report_starts_at_the_month_of_the_first_goal()
    {
        StoreGoal(4000m, new DateOnly(2026, 8, 31));

        var report = (await RunAsync(months: null)).Value;

        report.Months[0].ShouldSatisfyAllConditions(
            month => month.Year.ShouldBe(2026),
            month => month.Month.ShouldBe(5));
        report.Months.Count.ShouldBe(5);
    }

    [Fact]
    public async Task Without_a_period_and_without_goals_the_last_six_months_are_shown()
    {
        var report = (await RunAsync(months: null)).Value;

        report.Months.Count.ShouldBe(6);
    }
}
