namespace FinGrow.Application.UnitTests.Features.Metrics;

using FinGrow.Application.Features.Metrics.FinancialHealth;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class FinancialHealthScorerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 4, 0, 0, TimeSpan.Zero);
    private static readonly MetricsPeriod July = MetricsPeriod.Of(2026, 7);
    private static readonly MetricsPeriod August = MetricsPeriod.Of(2026, 8);
    private static readonly Guid CompanyId = Guid.CreateVersion7();

    private readonly FakeFinancialHealthReadRepository _finances = new();
    private readonly FakeGoalRepository _goals = new();
    private readonly FakeFinancialHealthScoreRepository _scores = new();
    private readonly FakeExchangeRateProvider _exchangeRates = new();

    [Fact]
    public async Task An_employee_with_enough_activity_gets_a_score_for_the_period()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(Pesos(employeeId, monthIncome: 100000m, monthSpending: 90000m));

        var scoring = await ScoreAsync(Activity(employeeId, confirmedTransactions: 12));

        scoring.IsComplete.ShouldBeTrue();
        scoring.Scores[employeeId].ShouldBe(64);
        var score = _scores.Scores.ShouldHaveSingleItem();
        score.EmployeeId.ShouldBe(employeeId);
        score.Period.ShouldBe(August);
        score.Factors.ShouldBe(FinancialHealthFactors.From(savings: 50m, budget: null, goals: null, cushion: 100m));
        score.Score.ShouldBe(64);
        score.ComputedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Fewer_than_ten_confirmed_transactions_get_no_score()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(Pesos(employeeId, monthIncome: 100000m, monthSpending: 90000m));

        var scoring = await ScoreAsync(Activity(employeeId, confirmedTransactions: 9));

        scoring.IsComplete.ShouldBeTrue();
        scoring.Scores.ShouldBeEmpty();
        _scores.Scores.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_month_without_income_gets_no_score_instead_of_a_zero()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(Pesos(employeeId, monthIncome: 0m, monthSpending: 90000m));

        var scoring = await ScoreAsync(Activity(employeeId, confirmedTransactions: 15));

        scoring.Scores.ShouldBeEmpty();
        _scores.Scores.ShouldBeEmpty();
    }

    [Fact]
    public async Task Budget_and_goals_count_when_the_employee_has_them()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(Pesos(employeeId, monthIncome: 100000m, monthSpending: 90000m));
        _finances.Limits.Add(new EmployeeBudgetLimit(employeeId, Ars(50000m), Ars(20000m)));
        _finances.Limits.Add(new EmployeeBudgetLimit(employeeId, Ars(30000m), Ars(35000m)));
        var goal = Goal.Create(employeeId, "Vacaciones", Ars(1000000m), new DateOnly(2026, 12, 31), new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero));
        goal.AddContribution(Ars(50000m), new DateOnly(2026, 8, 10), null, Now);
        _goals.Goals.Add(goal);

        var scoring = await ScoreAsync(Activity(employeeId, confirmedTransactions: 12));

        _scores.Scores.ShouldHaveSingleItem().Factors
            .ShouldBe(FinancialHealthFactors.From(savings: 50m, budget: 50m, goals: 50m, cushion: 100m));
        scoring.Scores[employeeId].ShouldBe(58);
    }

    [Fact]
    public async Task Another_employee_budget_and_goals_do_not_leak_into_the_score()
    {
        var employeeId = Guid.CreateVersion7();
        var colleagueId = Guid.CreateVersion7();
        _finances.Totals.Add(Pesos(employeeId, monthIncome: 100000m, monthSpending: 90000m));
        _finances.Totals.Add(Pesos(colleagueId, monthIncome: 100000m, monthSpending: 90000m));
        _finances.Limits.Add(new EmployeeBudgetLimit(colleagueId, Ars(30000m), Ars(35000m)));

        await ScoreAsync(Activity(employeeId, confirmedTransactions: 12), Activity(colleagueId, confirmedTransactions: 12));

        _scores.Scores.Single(score => score.EmployeeId == employeeId).Factors.Budget.ShouldBeNull();
        _scores.Scores.Single(score => score.EmployeeId == colleagueId).Factors.Budget.ShouldBe(0m);
    }

    [Fact]
    public async Task Dollar_movements_are_converted_to_pesos_at_the_mep_sell_rate()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(new EmployeeCurrencyTotals(employeeId, Currency.ARS, new DateOnly(2026, 8, 1), 0m, 140157m, 0m, 140157m, 140157m));
        _finances.Totals.Add(new EmployeeCurrencyTotals(employeeId, Currency.USD, new DateOnly(2026, 8, 5), 100m, 0m, 100m, 0m, 0m));

        await ScoreAsync(Activity(employeeId, confirmedTransactions: 10));

        var factors = _scores.Scores.ShouldHaveSingleItem().Factors;
        factors.Savings.ShouldBe(50m);
        factors.Cushion.ShouldBe(3.70m);
    }

    [Fact]
    public async Task Other_currencies_do_not_count()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(new EmployeeCurrencyTotals(employeeId, Currency.EUR, new DateOnly(2026, 8, 1), 3000m, 1000m, 3000m, 1000m, 1000m));

        var scoring = await ScoreAsync(Activity(employeeId, confirmedTransactions: 10));

        scoring.Scores.ShouldBeEmpty();
    }

    [Fact]
    public async Task Without_the_mep_rate_a_period_with_dollars_is_not_scored_and_nothing_is_saved()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(Pesos(employeeId, monthIncome: 100000m, monthSpending: 90000m));
        _finances.Totals.Add(new EmployeeCurrencyTotals(employeeId, Currency.USD, new DateOnly(2026, 8, 5), 100m, 0m, 100m, 0m, 0m));
        _exchangeRates.Failure = new HttpRequestException("DolarApi no responde");

        var scoring = await ScoreAsync(Activity(employeeId, confirmedTransactions: 12));

        scoring.IsComplete.ShouldBeFalse();
        scoring.Scores.ShouldBeEmpty();
        _scores.Scores.ShouldBeEmpty();
    }

    [Fact]
    public async Task Without_the_mep_rate_a_period_without_dollars_is_scored_anyway()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(Pesos(employeeId, monthIncome: 100000m, monthSpending: 90000m));
        _exchangeRates.Failure = new HttpRequestException("DolarApi no responde");

        var scoring = await ScoreAsync(Activity(employeeId, confirmedTransactions: 12));

        scoring.IsComplete.ShouldBeTrue();
        scoring.Scores.ShouldContainKey(employeeId);
        _exchangeRates.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task The_mep_rate_is_asked_once_per_run()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(new EmployeeCurrencyTotals(employeeId, Currency.USD, new DateOnly(2026, 7, 5), 100m, 50m, 100m, 50m, 50m));
        var scorer = Scorer();
        var activity = new[] { Activity(employeeId, confirmedTransactions: 10) };

        await scorer.ScoreAsync(CompanyId, July, activity, Now, CancellationToken.None);
        await scorer.ScoreAsync(CompanyId, August, activity, Now, CancellationToken.None);

        _exchangeRates.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Rescoring_a_period_refreshes_the_scores_and_drops_the_ones_that_no_longer_qualify()
    {
        var stillActive = Guid.CreateVersion7();
        var noLongerActive = Guid.CreateVersion7();
        var earlier = Now.AddDays(-20);
        var previous = FinancialHealthScore.Compute(stillActive, August, FinancialHealthFactors.From(0m, null, null, 0m), earlier);
        _scores.Scores.Add(previous);
        _scores.Scores.Add(FinancialHealthScore.Compute(noLongerActive, August, FinancialHealthFactors.From(0m, null, null, 0m), earlier));
        _finances.Totals.Add(Pesos(stillActive, monthIncome: 100000m, monthSpending: 90000m));
        _finances.Totals.Add(Pesos(noLongerActive, monthIncome: 100000m, monthSpending: 90000m));

        await ScoreAsync(Activity(stillActive, confirmedTransactions: 12), Activity(noLongerActive, confirmedTransactions: 3));

        var score = _scores.Scores.ShouldHaveSingleItem();
        score.ShouldBeSameAs(previous);
        score.Score.ShouldBe(64);
        score.ComputedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Spending_is_averaged_over_the_months_since_the_first_movement_up_to_three()
    {
        var employeeId = Guid.CreateVersion7();
        _finances.Totals.Add(new EmployeeCurrencyTotals(employeeId, Currency.ARS, new DateOnly(2026, 7, 20), 100000m, 90000m, 350000m, 200000m, 200000m));

        await ScoreAsync(Activity(employeeId, confirmedTransactions: 12));

        _scores.Scores.ShouldHaveSingleItem().Factors.Cushion.ShouldBe(50m);
    }

    private Task<FinancialHealthScoring> ScoreAsync(params EmployeePeriodActivity[] activity) =>
        Scorer().ScoreAsync(CompanyId, August, activity, Now, CancellationToken.None);

    private FinancialHealthScorer Scorer() => new(
        _finances, _goals, _scores, _exchangeRates, NullLogger<FinancialHealthScorer>.Instance);

    private static EmployeeCurrencyTotals Pesos(Guid employeeId, decimal monthIncome, decimal monthSpending) =>
        new(employeeId, Currency.ARS, new DateOnly(2026, 1, 10), monthIncome, monthSpending, 1000000m, 700000m, 300000m);

    private static EmployeePeriodActivity Activity(Guid employeeId, int confirmedTransactions) =>
        new(employeeId, null, confirmedTransactions, 0, false, false, 0, false);

    private static Money Ars(decimal amount) => Money.From(amount, Currency.ARS);
}
