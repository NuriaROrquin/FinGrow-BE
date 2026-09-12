namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class InvestmentTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void When_created_the_investment_is_worth_what_was_put_in()
    {
        var investment = CreateInvestment();

        investment.CurrentValue.ShouldBe(investment.InvestedAmount);
        investment.ReturnAmount.ShouldBe(0m);
        investment.ReturnPercentage.ShouldBe(0m);
    }

    [Fact]
    public void Positive_return_is_calculated_on_the_invested_capital()
    {
        var investment = CreateInvestment();

        investment.UpdateValuation(Money.From(1250m, Currency.USD), Now.AddMonths(6));

        investment.ReturnAmount.ShouldBe(250m);
        investment.ReturnPercentage.ShouldBe(25m);
    }

    [Fact]
    public void An_investment_can_result_in_a_loss()
    {
        var investment = CreateInvestment();

        investment.UpdateValuation(Money.From(800m, Currency.USD), Now.AddMonths(6));

        investment.ReturnAmount.ShouldBe(-200m);
        investment.ReturnPercentage.ShouldBe(-20m);
    }

    [Fact]
    public void The_valuation_must_be_in_the_same_currency_as_the_capital()
    {
        var investment = CreateInvestment();

        Should.Throw<DomainException>(() =>
            investment.UpdateValuation(Money.From(1250m, Currency.ARS), Now));
    }

    [Fact]
    public void Adding_capital_increases_the_invested_amount_and_current_value_without_inventing_return()
    {
        var investment = CreateInvestment();

        investment.AddCapital(Money.From(500m, Currency.USD), Now.AddMonths(1));

        investment.InvestedAmount.Amount.ShouldBe(1500m);
        investment.CurrentValue.Amount.ShouldBe(1500m);
        investment.ReturnAmount.ShouldBe(0m);
    }

    private static Investment CreateInvestment() => Investment.Create(
        EmployeeId,
        "S&P 500 ETF",
        InvestmentType.Etf,
        Money.From(1000m, Currency.USD),
        new DateOnly(2026, 1, 10),
        Now);
}
