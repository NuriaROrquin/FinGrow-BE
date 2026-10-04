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

    [Fact]
    public void Correcting_the_purchase_rewrites_the_initial_valuation_so_there_is_still_no_invented_return()
    {
        var investment = CreateInvestment();
        var correctedOn = new DateOnly(2026, 1, 5);

        investment.Correct("Bitcoin", InvestmentType.Crypto, Money.From(800m, Currency.ARS), correctedOn, Now.AddDays(1));

        investment.AssetName.ShouldBe("Bitcoin");
        investment.Type.ShouldBe(InvestmentType.Crypto);
        investment.InvestedAmount.ShouldBe(Money.From(800m, Currency.ARS));
        investment.PurchasedOn.ShouldBe(correctedOn);
        investment.CurrentValue.ShouldBe(Money.From(800m, Currency.ARS));
        investment.ValuedOn.ShouldBe(correctedOn);
        investment.ReturnAmount.ShouldBe(0m);
        investment.UpdatedAt.ShouldBe(Now.AddDays(1));
        investment.Valuations.ShouldHaveSingleItem();
    }

    [Fact]
    public void With_later_valuations_only_the_name_and_type_can_be_corrected()
    {
        var investment = CreateInvestment();
        investment.RecordValuation(Money.From(1250m, Currency.USD), new DateOnly(2026, 7, 10), ValuationSource.Feed, Now);

        investment.Correct("VOO", InvestmentType.Stock, Money.From(1000m, Currency.USD), PurchasedOn, Now);

        investment.AssetName.ShouldBe("VOO");
        investment.Type.ShouldBe(InvestmentType.Stock);
        Should.Throw<DomainException>(() =>
            investment.Correct("VOO", InvestmentType.Stock, Money.From(900m, Currency.USD), PurchasedOn, Now));
    }

    [Fact]
    public void A_correction_to_zero_capital_is_rejected()
    {
        var investment = CreateInvestment();

        Should.Throw<DomainException>(() =>
            investment.Correct("VOO", InvestmentType.Etf, Money.From(0m, Currency.USD), PurchasedOn, Now));
    }

    [Fact]
    public void A_new_investment_is_valued_at_cost_until_the_market_quotes_it()
    {
        var investment = CreateInvestment();

        investment.HasMarketValuation.ShouldBeFalse();
    }

    [Fact]
    public void A_valuation_from_the_feed_makes_the_investment_quoted()
    {
        var investment = CreateInvestment();

        investment.RecordValuation(Money.From(1100m, Currency.USD), new DateOnly(2026, 7, 10), ValuationSource.Feed, Now);

        investment.HasMarketValuation.ShouldBeTrue();
    }

    [Fact]
    public void Manual_valuations_and_added_capital_do_not_make_the_investment_quoted()
    {
        var investment = CreateInvestment();

        investment.RecordValuation(Money.From(1100m, Currency.USD), new DateOnly(2026, 7, 10), ValuationSource.Manual, Now);
        investment.AddCapital(Money.From(500m, Currency.USD), new DateOnly(2026, 8, 10), Now);

        investment.HasMarketValuation.ShouldBeFalse();
    }

    [Fact]
    public void Adding_capital_after_a_market_valuation_keeps_the_investment_quoted()
    {
        var investment = CreateInvestment();

        investment.RecordValuation(Money.From(1100m, Currency.USD), new DateOnly(2026, 7, 10), ValuationSource.Feed, Now);
        investment.AddCapital(Money.From(500m, Currency.USD), new DateOnly(2026, 8, 10), Now);

        investment.HasMarketValuation.ShouldBeTrue();
        investment.CurrentValue.Amount.ShouldBe(1600m);
    }

    [Fact]
    public void Tracking_stores_the_symbol_in_uppercase_with_its_quantity()
    {
        var investment = CreateInvestment();

        investment.Track("  spy ", 5m, Now);

        investment.Symbol.ShouldBe("SPY");
        investment.Quantity.ShouldBe(5m);
    }

    [Fact]
    public void Tracking_can_be_removed_by_clearing_both_values()
    {
        var investment = CreateInvestment();
        investment.Track("SPY", 5m, Now);

        investment.Track(null, null, Now);

        investment.Symbol.ShouldBeNull();
        investment.Quantity.ShouldBeNull();
    }

    [Theory]
    [InlineData("SPY", null)]
    [InlineData(null, 5.0)]
    [InlineData("   ", 5.0)]
    public void A_symbol_without_quantity_or_a_quantity_without_symbol_is_rejected(string? symbol, double? quantity)
    {
        var investment = CreateInvestment();

        Should.Throw<DomainException>(() => investment.Track(symbol, (decimal?)quantity, Now));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void A_quantity_that_is_not_positive_is_rejected(double quantity)
    {
        var investment = CreateInvestment();

        Should.Throw<DomainException>(() => investment.Track("SPY", (decimal)quantity, Now));
    }

    [Fact]
    public void A_symbol_longer_than_the_maximum_is_rejected()
    {
        var investment = CreateInvestment();

        Should.Throw<DomainException>(() => investment.Track(new string('A', Investment.MaxSymbolLength + 1), 1m, Now));
    }

    [Theory]
    [InlineData(InvestmentType.MutualFund)]
    [InlineData(InvestmentType.Crypto)]
    [InlineData(InvestmentType.FixedTermDeposit)]
    [InlineData(InvestmentType.Repo)]
    [InlineData(InvestmentType.RemuneratedAccount)]
    public void Assets_that_do_not_trade_on_the_exchange_cannot_be_tracked_by_symbol(InvestmentType type)
    {
        var investment = Investment.Create(EmployeeId, "Activo", type, Money.From(1000m, Currency.USD), PurchasedOn, Now);

        Should.Throw<DomainException>(() => investment.Track("BTC", 1m, Now));
    }

    [Theory]
    [InlineData(InvestmentType.Stock, "YPFD")]
    [InlineData(InvestmentType.Cedear, "AAPL")]
    [InlineData(InvestmentType.Etf, "SPY")]
    [InlineData(InvestmentType.Bond, "AL30")]
    [InlineData(InvestmentType.CorporateBond, "YMCXO")]
    [InlineData(InvestmentType.TreasuryBill, "S30N6")]
    public void Assets_that_trade_on_the_exchange_can_be_tracked_by_symbol(InvestmentType type, string symbol)
    {
        var investment = Investment.Create(EmployeeId, "Activo", type, Money.From(1000m, Currency.ARS), PurchasedOn, Now);

        investment.Track(symbol, 100m, Now);

        investment.Symbol.ShouldBe(symbol);
        investment.Quantity.ShouldBe(100m);
    }

    [Fact]
    public void A_market_valuation_is_recorded_as_coming_from_the_feed()
    {
        var investment = CreateInvestment();

        investment.RecordMarketValuation(Money.From(1100m, Currency.USD), new DateOnly(2026, 7, 10), Now);

        investment.Valuations.Count.ShouldBe(2);
        investment.LatestValuation.Source.ShouldBe(ValuationSource.Feed);
        investment.CurrentValue.Amount.ShouldBe(1100m);
        investment.HasMarketValuation.ShouldBeTrue();
    }

    [Fact]
    public void A_second_market_valuation_on_the_same_day_replaces_the_first()
    {
        var investment = CreateInvestment();
        var valuedOn = new DateOnly(2026, 7, 10);

        investment.RecordMarketValuation(Money.From(1100m, Currency.USD), valuedOn, Now);
        investment.RecordMarketValuation(Money.From(1150m, Currency.USD), valuedOn, Now.AddHours(1));

        investment.Valuations.Count.ShouldBe(2);
        investment.CurrentValue.Amount.ShouldBe(1150m);
    }

    [Fact]
    public void Market_valuations_on_different_days_accumulate_as_history()
    {
        var investment = CreateInvestment();

        investment.RecordMarketValuation(Money.From(1100m, Currency.USD), new DateOnly(2026, 7, 10), Now);
        investment.RecordMarketValuation(Money.From(1150m, Currency.USD), new DateOnly(2026, 7, 11), Now);

        investment.Valuations.Count.ShouldBe(3);
        investment.CurrentValue.Amount.ShouldBe(1150m);
    }

    [Fact]
    public void A_market_valuation_in_another_currency_is_rejected_even_when_it_replaces_one()
    {
        var investment = CreateInvestment();
        var valuedOn = new DateOnly(2026, 7, 10);
        investment.RecordMarketValuation(Money.From(1100m, Currency.USD), valuedOn, Now);

        Should.Throw<DomainException>(() =>
            investment.RecordMarketValuation(Money.From(1700000m, Currency.ARS), valuedOn, Now));
        Should.Throw<DomainException>(() =>
            investment.RecordMarketValuation(Money.From(1700000m, Currency.ARS), valuedOn.AddDays(1), Now));
    }

    private static Investment CreateInvestment() => Investment.Create(
        EmployeeId,
        "S&P 500 ETF",
        InvestmentType.Etf,
        Money.From(1000m, Currency.USD),
        PurchasedOn,
        Now);
}
