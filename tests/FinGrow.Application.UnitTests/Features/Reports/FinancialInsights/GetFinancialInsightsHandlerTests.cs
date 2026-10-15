namespace FinGrow.Application.UnitTests.Features.Reports.FinancialInsights;

using FinGrow.Application.Features.Reports.FinancialInsights;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Application.Validations.Reports;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class GetFinancialInsightsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 15, 0, 0, TimeSpan.Zero);

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeMonthlySummaryReadRepository _movements = new();
    private readonly FakeAchievementRepository _achievements = new();
    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeBudgetSpendingReadRepository _spending = new();
    private readonly FakeGoalRepository _goals = new();
    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeAiService _ai = new();

    private async Task<FinancialInsightsResponse> RunAsync(DateOnly from, DateOnly to, Currency currency = Currency.ARS, Guid? employeeId = null)
    {
        var handler = new GetFinancialInsightsHandler(
            _movements,
            _achievements,
            _budgets,
            _spending,
            _goals,
            _employees,
            _ai,
            new FakeDateTimeProvider(Now),
            NullLogger<GetFinancialInsightsHandler>.Instance);

        var result = await handler.Handle(
            new GetFinancialInsightsQuery(employeeId ?? _employeeId, currency, from, to),
            CancellationToken.None);

        return result.Value;
    }

    private void ActiveMonth(int year, int month, decimal income = 1000m, decimal spentPerMovement = 50m, Guid? employeeId = null)
    {
        var owner = employeeId ?? _employeeId;
        _movements.Income(owner, new DateOnly(year, month, 1), income);

        for (var day = 2; day <= FinancialHealthFactors.MinimumConfirmedTransactions; day++)
        {
            _movements.Expense(owner, new DateOnly(year, month, day), spentPerMovement, ExpenseCategory.Alimentos);
        }
    }

    [Fact]
    public async Task With_a_single_active_month_it_says_what_is_missing_and_does_not_call_the_AI()
    {
        ActiveMonth(2026, 9);
        _movements.Expense(_employeeId, new DateOnly(2026, 10, 3), 100m, ExpenseCategory.Transporte);
        _movements.Expense(_employeeId, new DateOnly(2026, 10, 4), 100m, ExpenseCategory.Transporte);

        var insights = await RunAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15));

        insights.Eligibility.ShouldBe(new InsightsEligibility(
            IsEligible: false,
            ActiveMonths: 1,
            RequiredActiveMonths: 2,
            CurrentMonthTransactions: 2,
            RequiredTransactions: 10,
            CurrentMonthHasIncome: false));
        insights.Highlights.ShouldBeNull();
        insights.Recommendations.ShouldBeEmpty();
        insights.RecommendationsAvailable.ShouldBeFalse();
        _ai.RecommendationRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Two_active_months_in_the_whole_history_are_enough_even_if_not_consecutive_and_one_is_the_current()
    {
        ActiveMonth(2026, 3);
        ActiveMonth(2026, 10);

        var insights = await RunAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15));

        insights.Eligibility.IsEligible.ShouldBeTrue();
        insights.Eligibility.ActiveMonths.ShouldBe(2);
        insights.Recommendations.Count.ShouldBe(3);
        insights.RecommendationsAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task Highlights_pick_the_months_of_the_period_and_count_ahorro_inversion_as_saved()
    {
        ActiveMonth(2026, 8, income: 1000m, spentPerMovement: 50m);
        ActiveMonth(2026, 9, income: 1500m, spentPerMovement: 100m);
        _movements.Income(_employeeId, new DateOnly(2026, 10, 1), 800m);
        _movements.Expense(_employeeId, new DateOnly(2026, 10, 2), 300m, ExpenseCategory.AhorroInversion);

        var highlights = (await RunAsync(new DateOnly(2026, 8, 1), new DateOnly(2026, 10, 15))).Highlights!;

        highlights.MonthCount.ShouldBe(3);
        highlights.HasMovements.ShouldBeTrue();
        highlights.TotalIncome.ShouldBe(3300m);
        highlights.TotalExpenses.ShouldBe(1650m);
        highlights.TotalSavings.ShouldBe(1950m);
        highlights.SavingsRate.ShouldBe(59.1m);
        highlights.HighestIncomeMonth.ShouldBe(new MonthAmount(2026, 9, 1500m));
        highlights.HighestExpenseMonth.ShouldBe(new MonthAmount(2026, 9, 900m));
        highlights.BestSavingsMonth.ShouldBe(new MonthAmount(2026, 10, 800m));
    }

    [Fact]
    public async Task The_AI_gets_three_reference_months_the_budget_of_the_last_month_the_goals_and_the_language()
    {
        ActiveMonth(2026, 9);
        ActiveMonth(2026, 10);
        var employee = Employee.Create(Guid.CreateVersion7(), null, "Ana Gomez", Email.From("ana@empresa.com"), null, "hash", Currency.ARS, new DateOnly(2025, 1, 1), Now);
        employee.UpdatePreferences(Theme.System, Language.en, Currency.ARS, DateFormat.DayMonthYear, Now);
        _employees.Add(employee);
        ActiveMonth(2026, 9, employeeId: employee.Id);
        ActiveMonth(2026, 10, employeeId: employee.Id);

        var budget = Budget.Create(employee.Id, BudgetPeriod.Monthly, new DateOnly(2026, 10, 1), Now);
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(400m, Currency.ARS), Now);
        _budgets.Add(budget);
        _spending.Spent[ExpenseCategory.Alimentos] = 450m;

        var trip = Goal.Create(employee.Id, "Vacaciones", Money.From(12000m, Currency.ARS), new DateOnly(2027, 6, 30), Now.AddMonths(-2));
        trip.AddContribution(Money.From(3000m, Currency.ARS), new DateOnly(2026, 9, 1), null, Now.AddMonths(-1));
        var cancelled = Goal.Create(employee.Id, "Moto", Money.From(5000m, Currency.ARS), new DateOnly(2027, 1, 31), Now);
        cancelled.Cancel(Now);
        _goals.Add(trip);
        _goals.Add(cancelled);
        _goals.Add(Goal.Create(employee.Id, "Viaje", Money.From(2000m, Currency.USD), new DateOnly(2027, 1, 31), Now));

        await RunAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 15), employeeId: employee.Id);

        var input = _ai.RecommendationRequests.ShouldHaveSingleItem();
        input.Language.ShouldBe(Language.en);
        input.Currency.ShouldBe(Currency.ARS);
        input.Months.Select(month => (month.Year, month.Month, month.IsReference)).ShouldBe(new[]
        {
            (2026, 6, true), (2026, 7, true), (2026, 8, true), (2026, 9, false), (2026, 10, false)
        });
        input.Months[3].ExpensesByCategory[ExpenseCategory.Alimentos].ShouldBe(450m);
        input.BudgetLimits.ShouldBe(new[] { new RecommendationBudgetLimit(ExpenseCategory.Alimentos, 400m, 450m) });
        var goal = input.Goals.ShouldHaveSingleItem();
        goal.Name.ShouldBe("Vacaciones");
        goal.CurrentAmount.ShouldBe(3000m);
        goal.IsAchieved.ShouldBeFalse();
    }

    [Fact]
    public async Task Without_preferences_the_recommendations_are_asked_in_spanish()
    {
        ActiveMonth(2026, 9);
        ActiveMonth(2026, 10);

        await RunAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15));

        _ai.RecommendationRequests.ShouldHaveSingleItem().Language.ShouldBe(Language.es);
        _ai.RecommendationRequests[0].BudgetLimits.ShouldBeEmpty();
    }

    [Fact]
    public async Task Only_achievements_earned_during_the_period_are_shown()
    {
        ActiveMonth(2026, 9);
        ActiveMonth(2026, 10);
        _achievements.Add(EmployeeAchievement.Backfill(_employeeId, AchievementCode.FirstTransaction, new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero), Now));
        _achievements.Add(EmployeeAchievement.Backfill(_employeeId, AchievementCode.Saver, new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero), Now));
        _achievements.Add(EmployeeAchievement.Backfill(_employeeId, AchievementCode.BudgetKept, new DateTimeOffset(2026, 9, 1, 2, 0, 0, TimeSpan.Zero), Now));

        var insights = await RunAsync(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 15));

        insights.Achievements.Select(achievement => achievement.Code).ShouldBe(new[] { AchievementCode.Saver });
        insights.Achievements[0].Name.ShouldBe("Ahorrador");
    }

    [Fact]
    public async Task If_the_AI_does_not_answer_the_highlights_still_come_without_recommendations()
    {
        ActiveMonth(2026, 9);
        ActiveMonth(2026, 10);
        _ai.Unreachable = true;

        var insights = await RunAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15));

        insights.Highlights.ShouldNotBeNull();
        insights.Recommendations.ShouldBeEmpty();
        insights.RecommendationsAvailable.ShouldBeFalse();
    }

    [Fact]
    public async Task A_period_without_movements_in_the_currency_does_not_call_the_AI()
    {
        ActiveMonth(2026, 9);
        ActiveMonth(2026, 10);

        var insights = await RunAsync(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 15), Currency.USD);

        insights.Highlights!.HasMovements.ShouldBeFalse();
        insights.Highlights.HighestIncomeMonth.ShouldBeNull();
        insights.Recommendations.ShouldBeEmpty();
        insights.RecommendationsAvailable.ShouldBeTrue();
        _ai.RecommendationRequests.ShouldBeEmpty();
    }

    [Fact]
    public void A_reversed_period_is_rejected()
    {
        var query = new GetFinancialInsightsQuery(_employeeId, Currency.ARS, new DateOnly(2026, 10, 15), new DateOnly(2026, 10, 1));

        new GetFinancialInsightsValidator().Validate(query).IsValid.ShouldBeFalse();
    }
}
