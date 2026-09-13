namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class BudgetTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void The_monthly_period_starts_on_the_first_of_the_month_even_when_created_midway()
    {
        var budget = CreateMonthlyBudget();

        budget.PeriodStart.ShouldBe(new DateOnly(2026, 3, 1));
        budget.PeriodEnd.ShouldBe(new DateOnly(2026, 3, 31));
    }

    [Fact]
    public void The_yearly_period_starts_on_the_first_of_january()
    {
        var budget = Budget.Create(EmployeeId, BudgetPeriod.Yearly, new DateOnly(2026, 3, 15), Now);

        budget.PeriodStart.ShouldBe(new DateOnly(2026, 1, 1));
        budget.PeriodEnd.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Fact]
    public void A_budget_is_created_without_limits_and_without_a_currency()
    {
        var budget = CreateMonthlyBudget();

        budget.Limits.ShouldBeEmpty();
        budget.Currency.ShouldBeNull();
        budget.LimitFor(ExpenseCategory.Alimentos).ShouldBeNull();
    }

    [Fact]
    public void Setting_a_limit_for_a_new_category_adds_it()
    {
        var budget = CreateMonthlyBudget();

        var limit = budget.SetLimit(ExpenseCategory.Alimentos, Money.From(50000m, Currency.ARS), Now);

        budget.Limits.ShouldHaveSingleItem().ShouldBe(limit);
        budget.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(50000m);
        budget.Currency.ShouldBe(Currency.ARS);
    }

    [Fact]
    public void Setting_a_limit_for_an_existing_category_changes_it_instead_of_duplicating_it()
    {
        var budget = CreateMonthlyBudget();
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(50000m, Currency.ARS), Now);

        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(60000m, Currency.ARS), Now.AddDays(1));

        budget.Limits.Count.ShouldBe(1);
        budget.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(60000m);
    }

    [Fact]
    public void A_zero_limit_is_rejected()
    {
        var budget = CreateMonthlyBudget();

        Should.Throw<DomainException>(() =>
            budget.SetLimit(ExpenseCategory.Alimentos, Money.Zero(Currency.ARS), Now));
    }

    [Fact]
    public void All_the_limits_of_a_budget_share_the_same_currency()
    {
        var budget = CreateMonthlyBudget();
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(50000m, Currency.ARS), Now);

        Should.Throw<DomainException>(() =>
            budget.SetLimit(ExpenseCategory.Transporte, Money.From(500m, Currency.USD), Now));
    }

    [Fact]
    public void Removing_a_limit_takes_it_out_of_the_budget()
    {
        var budget = CreateMonthlyBudget();
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(50000m, Currency.ARS), Now);

        budget.RemoveLimit(ExpenseCategory.Alimentos, Now.AddDays(1));

        budget.Limits.ShouldBeEmpty();
    }

    [Fact]
    public void Removing_a_limit_that_does_not_exist_fails()
    {
        var budget = CreateMonthlyBudget();

        Should.Throw<DomainException>(() => budget.RemoveLimit(ExpenseCategory.Alimentos, Now));
    }

    [Theory]
    [InlineData(0, BudgetHealth.OnTrack)]
    [InlineData(39999, BudgetHealth.OnTrack)]
    [InlineData(40000, BudgetHealth.Warning)]
    [InlineData(49999, BudgetHealth.Warning)]
    [InlineData(50000, BudgetHealth.Exceeded)]
    [InlineData(60000, BudgetHealth.Exceeded)]
    public void The_category_status_follows_the_eighty_and_hundred_percent_thresholds(
        decimal spent,
        BudgetHealth expected)
    {
        var budget = CreateMonthlyBudget();
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(50000m, Currency.ARS), Now);

        budget.Evaluate(ExpenseCategory.Alimentos, Money.From(spent, Currency.ARS)).ShouldBe(expected);
    }

    [Fact]
    public void Evaluating_a_category_without_a_limit_fails()
    {
        var budget = CreateMonthlyBudget();

        Should.Throw<DomainException>(() =>
            budget.Evaluate(ExpenseCategory.Alimentos, Money.From(100m, Currency.ARS)));
    }

    [Fact]
    public void The_budget_covers_the_dates_within_its_period()
    {
        var budget = CreateMonthlyBudget();

        budget.Covers(new DateOnly(2026, 3, 31)).ShouldBeTrue();
        budget.Covers(new DateOnly(2026, 4, 1)).ShouldBeFalse();
    }

    [Fact]
    public void Duplicating_a_budget_copies_its_limits_into_the_new_period()
    {
        var budget = CreateMonthlyBudget();
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(50000m, Currency.ARS), Now);
        budget.SetLimit(ExpenseCategory.Transporte, Money.From(20000m, Currency.ARS), Now);

        var next = budget.Duplicate(new DateOnly(2026, 4, 20), Now.AddMonths(1));

        next.Id.ShouldNotBe(budget.Id);
        next.EmployeeId.ShouldBe(EmployeeId);
        next.PeriodStart.ShouldBe(new DateOnly(2026, 4, 1));
        next.Limits.Count.ShouldBe(2);
        next.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(50000m);
        next.LimitFor(ExpenseCategory.Transporte)!.Amount.ShouldBe(20000m);
        next.Limits.ShouldAllBe(limit => limit.BudgetId == next.Id);
    }

    [Fact]
    public void Duplicating_a_budget_into_its_own_period_fails()
    {
        var budget = CreateMonthlyBudget();

        Should.Throw<DomainException>(() => budget.Duplicate(new DateOnly(2026, 3, 1), Now));
    }

    private static Budget CreateMonthlyBudget() =>
        Budget.Create(EmployeeId, BudgetPeriod.Monthly, new DateOnly(2026, 3, 15), Now);
}
