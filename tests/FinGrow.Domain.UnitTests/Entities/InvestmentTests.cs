namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class InvestmentTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PurchasedOn = new(2026, 1, 10);

    [Fact]
    public void When_created_the_investment_is_worth_what_was_put_in_as_of_the_purchase_date()
    {
        var investment = CreateInvestment();

        investment.CurrentValue.ShouldBe(investment.InvestedAmount);
        investment.ValuedOn.ShouldBe(PurchasedOn);
        investment.ReturnAmount.ShouldBe(0m);
        investment.ReturnPercentage.ShouldBe(0m);

        var initial = investment.Valuations.ShouldHaveSingleItem();
        initial.Source.ShouldBe(ValuationSource.Manual);
    }

    [Fact]
    public void The_current_value_is_the_latest_valuation()
    {
        var investment = CreateInvestment();

        investment.RecordValuation(Money.From(1250m, Currency.USD), new DateOnly(2026, 7, 10), ValuationSource.Feed, Now);
        investment.RecordValuation(Money.From(1100m, Currency.USD), new DateOnly(2026, 4, 10), ValuationSource.Feed, Now);

        investment.Valuations.Count.ShouldBe(3);
        investment.CurrentValue.Amount.ShouldBe(1250m);
        investment.ValuedOn.ShouldBe(new DateOnly(2026, 7, 10));
    }

    [Fact]
    public void Between_two_valuations_of_the_same_day_the_most_recently_recorded_wins()
    {
        var investment = CreateInvestment();
        var valuedOn = new DateOnly(2026, 7, 10);

        investment.RecordValuation(Money.From(1250m, Currency.USD), valuedOn, ValuationSource.Feed, Now);
        investment.RecordValuation(Money.From(1300m, Currency.USD), valuedOn, ValuationSource.Manual, Now.AddMinutes(5));

        investment.CurrentValue.Amount.ShouldBe(1300m);
        investment.LatestValuation.Source.ShouldBe(ValuationSource.Manual);
    }

    [Fact]
    public void Positive_return_is_calculated_on_the_invested_capital()
    {
        var investment = CreateInvestment();

        investment.RecordValuation(Money.From(1250m, Currency.USD), new DateOnly(2026, 7, 10), ValuationSource.Feed, Now);

        investment.ReturnAmount.ShouldBe(250m);
        investment.ReturnPercentage.ShouldBe(25m);
    }

    [Fact]
    public void An_investment_can_result_in_a_loss()
    {
        var investment = CreateInvestment();

        investment.RecordValuation(Money.From(800m, Currency.USD), new DateOnly(2026, 7, 10), ValuationSource.Feed, Now);

        investment.ReturnAmount.ShouldBe(-200m);
        investment.ReturnPercentage.ShouldBe(-20m);
    }

    [Fact]
    public void The_valuation_must_be_in_the_same_currency_as_the_capital()
    {
        var investment = CreateInvestment();

        Should.Throw<DomainException>(() =>
            investment.RecordValuation(Money.From(1250m, Currency.ARS), new DateOnly(2026, 7, 10), ValuationSource.Feed, Now));
    }

    [Fact]
    public void A_valuation_before_the_purchase_is_rejected()
    {
        var investment = CreateInvestment();

        Should.Throw<DomainException>(() =>
            investment.RecordValuation(Money.From(1250m, Currency.USD), PurchasedOn.AddDays(-1), ValuationSource.Feed, Now));
    }

    [Fact]
    public void Adding_capital_increases_the_invested_amount_and_current_value_without_inventing_return()
    {
        var investment = CreateInvestment();

        investment.AddCapital(Money.From(500m, Currency.USD), new DateOnly(2026, 2, 10), Now.AddMonths(1));

        investment.InvestedAmount.Amount.ShouldBe(1500m);
        investment.CurrentValue.Amount.ShouldBe(1500m);
        investment.ReturnAmount.ShouldBe(0m);
        investment.Valuations.Count.ShouldBe(2);
    }

    private static Investment CreateInvestment() => Investment.Create(
        EmployeeId,
        "S&P 500 ETF",
        InvestmentType.Etf,
        Money.From(1000m, Currency.USD),
        PurchasedOn,
        Now);
}
