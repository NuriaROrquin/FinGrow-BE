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
        var budget = CreateMonthlyBudget(50000m);

        budget.PeriodStart.ShouldBe(new DateOnly(2026, 3, 1));
        budget.PeriodEnd.ShouldBe(new DateOnly(2026, 3, 31));
    }

    [Fact]
    public void The_yearly_period_starts_on_the_first_of_january()
    {
        var budget = Budget.Create(
            EmployeeId,
            ExpenseCategory.Educacion,
            Money.From(600000m, Currency.ARS),
            BudgetPeriod.Yearly,
            new DateOnly(2026, 3, 15),
            Now);

        budget.PeriodStart.ShouldBe(new DateOnly(2026, 1, 1));
        budget.PeriodEnd.ShouldBe(new DateOnly(2026, 12, 31));
    }

    [Theory]
    [InlineData(0, BudgetHealth.OnTrack)]
    [InlineData(39999, BudgetHealth.OnTrack)]
    [InlineData(40000, BudgetHealth.Warning)]
    [InlineData(49999, BudgetHealth.Warning)]
    [InlineData(50000, BudgetHealth.Exceeded)]
    [InlineData(60000, BudgetHealth.Exceeded)]
    public void The_budget_status_follows_the_eighty_and_hundred_percent_thresholds(
        decimal spent,
        BudgetHealth expected)
    {
        var budget = CreateMonthlyBudget(50000m);

        budget.Evaluate(Money.From(spent, Currency.ARS)).ShouldBe(expected);
    }

    [Fact]
    public void A_budget_with_a_zero_limit_is_not_created()
    {
        Should.Throw<DomainException>(() => CreateMonthlyBudget(0m));
    }

    [Fact]
    public void The_currency_of_an_already_created_budget_cannot_be_changed()
    {
        var budget = CreateMonthlyBudget(50000m);

        Should.Throw<DomainException>(() => budget.ChangeLimit(Money.From(500m, Currency.USD), Now));
    }

    [Fact]
    public void The_budget_covers_the_dates_within_its_period()
    {
        var budget = CreateMonthlyBudget(50000m);

        budget.Covers(new DateOnly(2026, 3, 31)).ShouldBeTrue();
        budget.Covers(new DateOnly(2026, 4, 1)).ShouldBeFalse();
    }

    private static Budget CreateMonthlyBudget(decimal limit) => Budget.Create(
        EmployeeId,
        ExpenseCategory.Alimentos,
        Money.From(limit, Currency.ARS),
        BudgetPeriod.Monthly,
        new DateOnly(2026, 3, 15),
        Now);
}
