namespace FinGrow.Application.UnitTests.Features.Budgets.Analysis;

using FinGrow.Application.Features.Budgets.Analysis;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Application.Validations.Budgets;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

public class GetBudgetAnalysisHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 15, 0, 0, TimeSpan.Zero);

    private static readonly bool[] ClosedThenCurrent = { false, true };

    private readonly Guid _employeeId = Guid.CreateVersion7();
    private readonly FakeBudgetRepository _budgets = new();
    private readonly FakeMonthlySummaryReadRepository _movements = new();
    private readonly FakeEmployeeRepository _employees = new();
    private readonly FakeAiService _ai = new();

    private async Task<BudgetAnalysisResponse> RunAsync(Currency currency = Currency.ARS)
    {
        var handler = new GetBudgetAnalysisHandler(
            _budgets,
            _movements,
            _employees,
            _ai,
            new FakeDateTimeProvider(Now),
            NullLogger<GetBudgetAnalysisHandler>.Instance);

        var result = await handler.Handle(new GetBudgetAnalysisQuery(_employeeId, currency), CancellationToken.None);

        return result.Value;
    }

    private void BudgetFor(int year, int month, params (ExpenseCategory Category, decimal Limit)[] limits) =>
        BudgetIn(Currency.ARS, year, month, limits);

    private void BudgetIn(Currency currency, int year, int month, params (ExpenseCategory Category, decimal Limit)[] limits)
    {
        var budget = Budget.Create(_employeeId, BudgetPeriod.Monthly, new DateOnly(year, month, 1), currency, Now);

        foreach (var (category, limit) in limits)
        {
            budget.SetLimit(category, Money.From(limit, currency), Now);
        }

        _budgets.Add(budget);
    }

    private void Spend(int year, int month, ExpenseCategory category, decimal amount, Currency currency = Currency.ARS) =>
        _movements.Expense(_employeeId, new DateOnly(year, month, 10), amount, category, currency);

    [Fact]
    public async Task With_a_single_budget_month_it_says_what_is_missing_and_does_not_call_the_AI()
    {
        BudgetFor(2026, 9, (ExpenseCategory.Alimentos, 1000m));

        var analysis = await RunAsync();

        analysis.Eligibility.ShouldBe(new BudgetAnalysisEligibility(
            IsEligible: false,
            BudgetMonths: 1,
            RequiredBudgetMonths: 2,
            AnalyzedMonths: 6,
            HasCurrentMonthBudget: false));
        analysis.Summary.ShouldBeNull();
        analysis.Recommendations.ShouldBeEmpty();
        analysis.RecommendationsAvailable.ShouldBeFalse();
        _ai.BudgetRecommendationRequests.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_current_month_budget_alone_is_not_enough()
    {
        BudgetFor(2026, 10, (ExpenseCategory.Alimentos, 1000m));

        var analysis = await RunAsync();

        analysis.Eligibility.IsEligible.ShouldBeFalse();
        analysis.Eligibility.BudgetMonths.ShouldBe(1);
        analysis.Eligibility.HasCurrentMonthBudget.ShouldBeTrue();
    }

    [Fact]
    public async Task One_closed_month_and_the_current_one_are_enough_but_the_summary_only_looks_at_the_closed_one()
    {
        BudgetFor(2026, 9, (ExpenseCategory.Alimentos, 1000m));
        BudgetFor(2026, 10, (ExpenseCategory.Alimentos, 100m));
        Spend(2026, 9, ExpenseCategory.Alimentos, 400m);
        Spend(2026, 10, ExpenseCategory.Alimentos, 500m);

        var analysis = await RunAsync();

        analysis.Eligibility.IsEligible.ShouldBeTrue();
        analysis.Eligibility.BudgetMonths.ShouldBe(2);
        analysis.Eligibility.HasCurrentMonthBudget.ShouldBeTrue();
        analysis.Summary!.ClosedBudgets.ShouldBe(1);
        analysis.Summary.FirstMonth.ShouldBe(new DateOnly(2026, 9, 1));
        analysis.Summary.LastMonth.ShouldBe(new DateOnly(2026, 9, 1));
        analysis.Summary.MostExceededCategory.ShouldBeNull();
        _ai.BudgetRecommendationRequests.ShouldHaveSingleItem().Months.Select(month => month.IsCurrent)
            .ShouldBe(ClosedThenCurrent);
    }

    [Fact]
    public async Task Only_budgets_in_the_requested_currency_and_inside_the_last_six_closed_months_count()
    {
        BudgetFor(2026, 3, (ExpenseCategory.Alimentos, 1000m));
        BudgetIn(Currency.USD, 2026, 8, (ExpenseCategory.Alimentos, 100m));
        BudgetFor(2026, 9, (ExpenseCategory.Alimentos, 1000m));
        BudgetFor(2026, 7);

        var analysis = await RunAsync();

        analysis.Eligibility.IsEligible.ShouldBeFalse();
        analysis.Eligibility.BudgetMonths.ShouldBe(1);
    }

    [Fact]
    public async Task The_summary_counts_exceeded_months_per_category_and_the_compliance_of_the_last_closed_month()
    {
        BudgetFor(2026, 8, (ExpenseCategory.Alimentos, 1000m), (ExpenseCategory.Transporte, 500m));
        BudgetFor(2026, 9, (ExpenseCategory.Alimentos, 1000m), (ExpenseCategory.Transporte, 500m));
        Spend(2026, 8, ExpenseCategory.Alimentos, 1200m);
        Spend(2026, 8, ExpenseCategory.Transporte, 200m);
        Spend(2026, 9, ExpenseCategory.Alimentos, 1000m);
        Spend(2026, 9, ExpenseCategory.Transporte, 300m);
        Spend(2026, 9, ExpenseCategory.Entretenimiento, 400m);

        var summary = (await RunAsync()).Summary!;

        summary.FirstMonth.ShouldBe(new DateOnly(2026, 8, 1));
        summary.LastMonth.ShouldBe(new DateOnly(2026, 9, 1));
        summary.ClosedBudgets.ShouldBe(2);
        summary.MostExceededCategory.ShouldBe(ExpenseCategory.Alimentos);
        summary.Categories.ShouldBe(new[]
        {
            new BudgetCategoryAnalysis(ExpenseCategory.Alimentos, 2, 2, 1000m, 1100m, 110m, new DateOnly(2026, 9, 1)),
            new BudgetCategoryAnalysis(ExpenseCategory.Transporte, 2, 0, 500m, 250m, 50m, null),
        });
        summary.LastClosedMonth.ShouldBe(new BudgetMonthCompliance(2026, 9, 2, 1, 1500m, 1300m));
    }

    [Fact]
    public async Task Going_over_the_ahorro_inversion_limit_is_not_the_most_exceeded_nor_breaks_compliance()
    {
        BudgetFor(2026, 8, (ExpenseCategory.AhorroInversion, 100m));
        BudgetFor(2026, 9, (ExpenseCategory.AhorroInversion, 100m), (ExpenseCategory.Alimentos, 1000m));
        Spend(2026, 8, ExpenseCategory.AhorroInversion, 300m);
        Spend(2026, 9, ExpenseCategory.AhorroInversion, 300m);
        Spend(2026, 9, ExpenseCategory.Alimentos, 500m);

        var summary = (await RunAsync()).Summary!;

        summary.MostExceededCategory.ShouldBeNull();
        summary.Categories[0].Category.ShouldBe(ExpenseCategory.AhorroInversion);
        summary.Categories[0].MonthsExceeded.ShouldBe(2);
        summary.LastClosedMonth.CategoriesWithLimit.ShouldBe(1);
        summary.LastClosedMonth.CategoriesWithinLimit.ShouldBe(1);
    }

    [Fact]
    public async Task The_AI_gets_every_month_with_its_limits_the_unbudgeted_spending_and_the_current_month_marked()
    {
        var employee = Employee.Create(Guid.CreateVersion7(), null, "Ana Gomez", Email.From("ana@empresa.com"), null, "hash", Currency.ARS, new DateOnly(2025, 1, 1), Now);
        employee.UpdatePreferences(Theme.System, Language.pt, Currency.ARS, DateFormat.DayMonthYear, Now);
        _employees.Add(employee);
        var ownerBudgets = new FakeBudgetRepository();
        foreach (var month in new[] { 8, 9, 10 })
        {
            var budget = Budget.Create(employee.Id, BudgetPeriod.Monthly, new DateOnly(2026, month, 1), Now);
            budget.SetLimit(ExpenseCategory.Alimentos, Money.From(1000m, Currency.ARS), Now);
            ownerBudgets.Add(budget);
        }

        _movements.Expense(employee.Id, new DateOnly(2026, 9, 5), 1100m, ExpenseCategory.Alimentos);
        _movements.Expense(employee.Id, new DateOnly(2026, 9, 6), 250m, ExpenseCategory.Transporte);
        _movements.Expense(employee.Id, new DateOnly(2026, 10, 2), 300m, ExpenseCategory.Alimentos);
        _movements.Expense(employee.Id, new DateOnly(2026, 9, 7), 999m, ExpenseCategory.Alimentos, Currency.USD);

        var handler = new GetBudgetAnalysisHandler(
            ownerBudgets,
            _movements,
            _employees,
            _ai,
            new FakeDateTimeProvider(Now),
            NullLogger<GetBudgetAnalysisHandler>.Instance);
        await handler.Handle(new GetBudgetAnalysisQuery(employee.Id, Currency.ARS), CancellationToken.None);

        var input = _ai.BudgetRecommendationRequests.ShouldHaveSingleItem();
        input.Language.ShouldBe(Language.pt);
        input.Currency.ShouldBe(Currency.ARS);
        input.Months.Select(month => (month.Year, month.Month, month.IsCurrent)).ShouldBe(new[]
        {
            (2026, 8, false), (2026, 9, false), (2026, 10, true)
        });
        input.Months[0].Limits.ShouldBe(new[] { new RecommendationBudgetLimit(ExpenseCategory.Alimentos, 1000m, 0m) });
        input.Months[1].Limits.ShouldBe(new[] { new RecommendationBudgetLimit(ExpenseCategory.Alimentos, 1000m, 1100m) });
        var unbudgeted = input.Months[1].UnbudgetedExpenses.ShouldHaveSingleItem();
        unbudgeted.Key.ShouldBe(ExpenseCategory.Transporte);
        unbudgeted.Value.ShouldBe(250m);
        input.Months[2].Limits.ShouldBe(new[] { new RecommendationBudgetLimit(ExpenseCategory.Alimentos, 1000m, 300m) });
    }

    [Fact]
    public async Task The_current_month_does_not_enter_the_summary()
    {
        BudgetFor(2026, 8, (ExpenseCategory.Alimentos, 1000m));
        BudgetFor(2026, 9, (ExpenseCategory.Alimentos, 1000m));
        BudgetFor(2026, 10, (ExpenseCategory.Alimentos, 100m));
        Spend(2026, 10, ExpenseCategory.Alimentos, 500m);

        var analysis = await RunAsync();

        analysis.Summary!.LastMonth.ShouldBe(new DateOnly(2026, 9, 1));
        analysis.Summary.MostExceededCategory.ShouldBeNull();
        analysis.Recommendations.Count.ShouldBe(3);
        analysis.RecommendationsAvailable.ShouldBeTrue();
    }

    [Fact]
    public async Task If_the_AI_does_not_answer_the_summary_still_comes_without_recommendations()
    {
        BudgetFor(2026, 8, (ExpenseCategory.Alimentos, 1000m));
        BudgetFor(2026, 9, (ExpenseCategory.Alimentos, 1000m));
        _ai.Unreachable = true;

        var analysis = await RunAsync();

        analysis.Eligibility.IsEligible.ShouldBeTrue();
        analysis.Summary.ShouldNotBeNull();
        analysis.Recommendations.ShouldBeEmpty();
        analysis.RecommendationsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void An_unknown_currency_is_rejected_by_the_validator()
    {
        var result = new GetBudgetAnalysisValidator().Validate(new GetBudgetAnalysisQuery(_employeeId, (Currency)99));

        result.IsValid.ShouldBeFalse();
    }
}
