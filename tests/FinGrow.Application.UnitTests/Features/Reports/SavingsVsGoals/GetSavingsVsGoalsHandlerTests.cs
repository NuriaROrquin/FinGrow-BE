namespace FinGrow.Application.UnitTests.Features.Reports.SavingsVsGoals;

using Common;
using FinGrow.Application.Features.Reports.SavingsVsGoals;
using FinGrow.Application.Interfaces;
using Fakes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

public class GetSavingsVsGoalsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 15, 0, 0, TimeSpan.Zero);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeGoalRepository _goals = new();
    private readonly FakeMonthlyTotalsRepository _transactions = new();
    private readonly GetSavingsVsGoalsHandler _handler;

    public GetSavingsVsGoalsHandlerTests() =>
        _handler = new GetSavingsVsGoalsHandler(_goals, _transactions, new FakeDateTimeProvider(Now));

    private Task<Result<SavingsVsGoalsResponse>> RunAsync(
        int? months = 6,
        Currency currency = Currency.ARS) =>
        _handler.Handle(new GetSavingsVsGoalsQuery(_employeeId, currency, months), CancellationToken.None);

    private Goal StoreMayToAugustGoal(Currency currency = Currency.ARS)
    {
        var goal = Goal.Create(
            _employeeId,
            "Vacaciones",
            Money.From(4000m, currency),
            new DateOnly(2026, 8, 31),
            new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));
        _goals.Add(goal);
        return goal;
    }

    [Fact]
    public async Task The_series_covers_the_requested_months_up_to_the_current_one()
    {
        _transactions.Totals.Add(new MonthlyTotals(2026, 9, Income: 1000m, Expense: 600m));

        var report = (await RunAsync(months: 6)).Value;

        report.Months.Select(month => (month.Year, month.Month)).ShouldBe(new[]
        {
            (2026, 4), (2026, 5), (2026, 6), (2026, 7), (2026, 8), (2026, 9)
        });
        report.Months[0].ActualSavings.ShouldBe(0m);
        report.Months[5].ActualSavings.ShouldBe(400m);
        _transactions.LastRequest.ShouldBe((Currency.ARS, new DateOnly(2026, 4, 1), new DateOnly(2026, 9, 30)));
    }

    [Fact]
    public async Task Each_month_compares_the_actual_savings_against_the_commitment_of_the_goals_in_force()
    {
        StoreMayToAugustGoal();
        _transactions.Totals.Add(new MonthlyTotals(2026, 5, Income: 3000m, Expense: 1500m));
        _transactions.Totals.Add(new MonthlyTotals(2026, 6, Income: 3000m, Expense: 2500m));

        var report = (await RunAsync(months: 6)).Value;

        var april = report.Months[0];
        april.Committed.ShouldBe(0m);
        april.MetTarget.ShouldBeNull();

        var may = report.Months[1];
        may.Committed.ShouldBe(1000m);
        may.ActualSavings.ShouldBe(1500m);
        may.MetTarget.ShouldBe(true);

        var june = report.Months[2];
        june.ActualSavings.ShouldBe(500m);
        june.MetTarget.ShouldBe(false);

        report.Months[5].Committed.ShouldBe(0m);
        report.MonthsWithCommitment.ShouldBe(4);
        report.MonthsOnTarget.ShouldBe(1);
        report.TotalCommitted.ShouldBe(4000m);
    }

    [Fact]
    public async Task An_achieved_goal_stops_committing_after_the_month_it_was_reached()
    {
        var goal = StoreMayToAugustGoal();
        goal.AddContribution(
            Money.From(4000m, Currency.ARS),
            new DateOnly(2026, 7, 5),
            null,
            new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero));

        var report = (await RunAsync(months: 6)).Value;

        report.Months.Single(month => month.Month == 7).Committed.ShouldBe(1000m);
        report.Months.Single(month => month.Month == 8).Committed.ShouldBe(0m);
    }

    [Fact]
    public async Task Goals_in_another_currency_are_left_out()
    {
        StoreMayToAugustGoal(Currency.USD);

        var report = (await RunAsync(months: 6, currency: Currency.ARS)).Value;

        report.HasGoals.ShouldBeFalse();
        report.TotalCommitted.ShouldBe(0m);
    }

    [Fact]
    public async Task The_savings_rate_is_the_share_of_income_that_was_saved()
    {
        _transactions.Totals.Add(new MonthlyTotals(2026, 8, Income: 2000m, Expense: 1500m));
        _transactions.Totals.Add(new MonthlyTotals(2026, 9, Income: 2000m, Expense: 1300m));

        var report = (await RunAsync(months: 6)).Value;

        report.TotalIncome.ShouldBe(4000m);
        report.TotalActualSavings.ShouldBe(1200m);
        report.SavingsRate.ShouldBe(30m);
    }

    [Fact]
    public async Task Without_income_there_is_no_savings_rate()
    {
        var report = (await RunAsync(months: 6)).Value;

        report.SavingsRate.ShouldBeNull();
    }

    [Fact]
    public async Task Without_a_period_the_report_starts_at_the_month_of_the_first_goal()
    {
        StoreMayToAugustGoal();

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
