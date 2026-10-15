namespace FinGrow.Application.UnitTests.Features.Achievements;

using FinGrow.Application.Features.Achievements.Evaluate;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class MonthlyAchievementsEvaluatorTests
{
    private static readonly Guid CompanyId = Guid.CreateVersion7();
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly MetricsPeriod September = MetricsPeriod.Of(2026, 9);

    private readonly FakePeriodActivityReadRepository _activity = new();
    private readonly FakeFinancialHealthReadRepository _finances = new();
    private readonly FakeExchangeRateProvider _exchangeRates = new();

    private MonthlyAchievementsEvaluator Evaluator => new(
        _activity, _finances, _exchangeRates, NullLogger<MonthlyAchievementsEvaluator>.Instance);

    [Fact]
    public async Task Three_active_months_in_a_row_earn_consistency_dated_at_the_month_close()
    {
        Active(MetricsPeriod.Of(2026, 7), MetricsPeriod.Of(2026, 8), September);
        Totals(Currency.ARS, income: 100_000m, spending: 95_000m);

        var result = await Evaluator.EvaluateAsync(CompanyId, September, CancellationToken.None);

        result.Reached[EmployeeId][AchievementCode.ConsistentThreeMonths].ShouldBe(September.EndInstantExclusive);
        result.Reached[EmployeeId].ShouldNotContainKey(AchievementCode.Saver);
    }

    [Fact]
    public async Task A_quiet_month_in_between_breaks_consistency()
    {
        Active(MetricsPeriod.Of(2026, 7), September);
        _activity.Activity[(CompanyId, MetricsPeriod.Of(2026, 8).Start)] = new() { Activity(confirmedTransactions: 4) };
        Totals(Currency.ARS, income: 100_000m, spending: 95_000m);

        var result = await Evaluator.EvaluateAsync(CompanyId, September, CancellationToken.None);

        result.Reached.ShouldNotContainKey(EmployeeId);
    }

    [Fact]
    public async Task Saving_ten_percent_of_income_earns_the_saver_achievement()
    {
        Active(September);
        Totals(Currency.ARS, income: 100_000m, spending: 90_000m);

        var result = await Evaluator.EvaluateAsync(CompanyId, September, CancellationToken.None);

        result.Reached[EmployeeId].Keys.ShouldBe(new[] { AchievementCode.Saver });
    }

    [Fact]
    public async Task Dollars_are_converted_at_the_mep_rate_before_measuring_savings()
    {
        Active(September);
        Totals(Currency.ARS, income: 100_000m, spending: 0m);
        Totals(Currency.USD, income: 0m, spending: 100m);
        _exchangeRates.Quote = _exchangeRates.Quote with { Sell = 1_000m };

        var result = await Evaluator.EvaluateAsync(CompanyId, September, CancellationToken.None);

        result.Reached.ShouldNotContainKey(EmployeeId);
        result.WithoutExchangeRate.ShouldBeEmpty();
    }

    [Fact]
    public async Task Without_the_mep_rate_savings_in_dollars_are_left_for_the_next_run()
    {
        Active(September);
        Totals(Currency.USD, income: 2_000m, spending: 100m);
        _exchangeRates.Failure = new HttpRequestException("DolarApi no responde.");

        var result = await Evaluator.EvaluateAsync(CompanyId, September, CancellationToken.None);

        result.Reached.ShouldNotContainKey(EmployeeId);
        result.WithoutExchangeRate.ShouldBe(new[] { EmployeeId });
    }

    [Fact]
    public async Task A_budget_is_kept_only_if_no_category_went_over()
    {
        var kept = Guid.CreateVersion7();
        var exceeded = Guid.CreateVersion7();
        _activity.Activity[(CompanyId, September.Start)] = new() { Activity(employeeId: kept), Activity(employeeId: exceeded) };
        Totals(Currency.ARS, income: 100_000m, spending: 100_000m, employeeId: kept);
        Totals(Currency.ARS, income: 100_000m, spending: 100_000m, employeeId: exceeded);
        Limit(kept, limit: 50_000m, spent: 49_000m);
        Limit(exceeded, limit: 50_000m, spent: 49_000m);
        Limit(exceeded, limit: 20_000m, spent: 25_000m);

        var result = await Evaluator.EvaluateAsync(CompanyId, September, CancellationToken.None);

        result.Reached[kept].Keys.ShouldBe(new[] { AchievementCode.BudgetKept });
        result.Reached.ShouldNotContainKey(exceeded);
    }

    [Fact]
    public async Task A_month_without_enough_activity_earns_nothing()
    {
        _activity.Activity[(CompanyId, September.Start)] = new() { Activity(confirmedTransactions: 9) };
        Totals(Currency.ARS, income: 100_000m, spending: 10_000m);
        Limit(EmployeeId, limit: 50_000m, spent: 1_000m);

        var result = await Evaluator.EvaluateAsync(CompanyId, September, CancellationToken.None);

        result.Reached.ShouldBeEmpty();
    }

    private void Active(params MetricsPeriod[] months)
    {
        foreach (var month in months)
        {
            _activity.Activity[(CompanyId, month.Start)] = new() { Activity() };
        }
    }

    private void Totals(Currency currency, decimal income, decimal spending, Guid? employeeId = null) =>
        _finances.Totals.Add(new EmployeeCurrencyTotals(
            employeeId ?? EmployeeId, currency, new DateOnly(2026, 1, 5), income, spending, income, spending, spending));

    private void Limit(Guid employeeId, decimal limit, decimal spent) =>
        _finances.Limits.Add(new EmployeeBudgetLimit(
            employeeId, Money.From(limit, Currency.ARS), Money.From(spent, Currency.ARS)));

    private static EmployeePeriodActivity Activity(int confirmedTransactions = 12, Guid? employeeId = null) =>
        new(employeeId ?? EmployeeId, null, confirmedTransactions, 0, false, false, 0, false);
}
