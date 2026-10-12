namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class ImportedInvestmentTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 13);
    private static readonly DateOnly PurchasedOn = new(2026, 9, 1);

    [Fact]
    public void An_investment_loaded_by_hand_is_manual_and_has_no_external_reference()
    {
        var investment = Investment.Create(EmployeeId, "S&P 500", InvestmentType.Etf, Money.From(1000m, Currency.USD), PurchasedOn, Now);

        investment.Source.ShouldBe(InvestmentSource.Manual);
        investment.ExternalReference.ShouldBeNull();
    }

    [Fact]
    public void An_imported_position_keeps_its_origin_symbol_and_quantity()
    {
        var investment = Import();

        investment.Source.ShouldBe(InvestmentSource.Iol);
        investment.ExternalReference.ShouldBe("bcba:GGAL");
        investment.Symbol.ShouldBe("GGAL");
        investment.Quantity.ShouldBe(10m);
        investment.InvestedAmount.ShouldBe(Money.From(50_000m, Currency.ARS));
        investment.PurchasedOn.ShouldBe(PurchasedOn);
    }

    [Fact]
    public void An_imported_position_starts_with_the_broker_valuation_of_the_day()
    {
        var investment = Import();

        var valuation = investment.Valuations.ShouldHaveSingleItem();
        valuation.Source.ShouldBe(ValuationSource.Feed);
        valuation.ValuedOn.ShouldBe(Today);
        investment.CurrentValue.ShouldBe(Money.From(56_000m, Currency.ARS));
        investment.ReturnAmount.ShouldBe(6_000m);
        investment.HasMarketValuation.ShouldBeTrue();
    }

    [Fact]
    public void A_position_bought_today_can_be_valued_today()
    {
        var investment = Import(purchasedOn: Today);

        investment.ValuedOn.ShouldBe(Today);
    }

    [Fact]
    public void An_import_has_to_come_from_an_integration()
    {
        Should.Throw<DomainException>(() => Import(source: InvestmentSource.Manual));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_import_needs_the_external_reference(string reference)
    {
        Should.Throw<DomainException>(() => Import(reference: reference));
    }

    [Fact]
    public void An_import_rejects_an_external_reference_that_does_not_fit()
    {
        Should.Throw<DomainException>(() => Import(reference: new string('x', Investment.MaxExternalReferenceLength + 1)));
    }

    [Fact]
    public void A_valuation_in_another_currency_is_rejected()
    {
        Should.Throw<DomainException>(() => Investment.Import(
            EmployeeId, InvestmentSource.Iol, "bcba:GGAL", "GGAL", InvestmentType.Stock, "GGAL", 10m,
            Money.From(50_000m, Currency.ARS), PurchasedOn, Money.From(40m, Currency.USD), Today, Now));
    }

    [Fact]
    public void A_new_quantity_adjusts_the_capital()
    {
        var investment = Import();

        var changed = investment.UpdatePosition(15m, Money.From(77_500m, Currency.ARS), Money.From(56_000m, Currency.ARS), Today.AddDays(1), Now.AddDays(1));

        changed.ShouldBeTrue();
        investment.Quantity.ShouldBe(15m);
        investment.InvestedAmount.ShouldBe(Money.From(77_500m, Currency.ARS));
        investment.Valuations.Count.ShouldBe(1);
    }

    [Fact]
    public void A_new_value_on_another_day_adds_a_valuation()
    {
        var investment = Import();

        var changed = investment.UpdatePosition(10m, Money.From(50_000m, Currency.ARS), Money.From(58_000m, Currency.ARS), Today.AddDays(1), Now.AddDays(1));

        changed.ShouldBeTrue();
        investment.Valuations.Count.ShouldBe(2);
        investment.CurrentValue.ShouldBe(Money.From(58_000m, Currency.ARS));
        investment.ValuedOn.ShouldBe(Today.AddDays(1));
        investment.InvestedAmount.ShouldBe(Money.From(50_000m, Currency.ARS));
    }

    [Fact]
    public void A_new_value_on_the_same_day_corrects_the_valuation_of_the_day()
    {
        var investment = Import();

        investment.UpdatePosition(10m, Money.From(50_000m, Currency.ARS), Money.From(57_000m, Currency.ARS), Today, Now.AddMinutes(30));

        investment.Valuations.ShouldHaveSingleItem().Value.ShouldBe(Money.From(57_000m, Currency.ARS));
    }

    [Fact]
    public void The_same_position_changes_nothing()
    {
        var investment = Import();

        var changed = investment.UpdatePosition(10m, Money.From(50_000m, Currency.ARS), Money.From(56_000m, Currency.ARS), Today.AddDays(1), Now.AddDays(1));

        changed.ShouldBeFalse();
        investment.Valuations.Count.ShouldBe(1);
        investment.UpdatedAt.ShouldBe(Now);
    }

    [Fact]
    public void A_manual_investment_is_never_overwritten_by_a_position()
    {
        var investment = Investment.Create(EmployeeId, "GGAL", InvestmentType.Stock, Money.From(50_000m, Currency.ARS), PurchasedOn, Now);

        Should.Throw<DomainException>(() =>
            investment.UpdatePosition(10m, Money.From(50_000m, Currency.ARS), Money.From(56_000m, Currency.ARS), Today, Now));
    }

    [Fact]
    public void A_position_cannot_change_currency()
    {
        var investment = Import();

        Should.Throw<DomainException>(() =>
            investment.UpdatePosition(10m, Money.From(40m, Currency.USD), Money.From(45m, Currency.USD), Today, Now));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("YPFD", 10)]
    [InlineData("GGAL", 12)]
    public void The_symbol_and_quantity_of_an_imported_position_cannot_be_changed_by_hand(string? symbol, int? quantity)
    {
        var investment = Import();

        Should.Throw<DomainException>(() => investment.Track(symbol, quantity, Now));
        investment.Symbol.ShouldBe("GGAL");
        investment.Quantity.ShouldBe(10m);
    }

    [Fact]
    public void An_imported_position_can_be_renamed()
    {
        var investment = Import();

        investment.Correct("Galicia", InvestmentType.Stock, Money.From(50_000m, Currency.ARS), PurchasedOn, Now);

        investment.AssetName.ShouldBe("Galicia");
        investment.CurrentValue.ShouldBe(Money.From(56_000m, Currency.ARS));
    }

    [Fact]
    public void The_capital_of_an_imported_position_cannot_be_changed_by_hand()
    {
        var investment = Import();

        Should.Throw<DomainException>(() =>
            investment.Correct("GGAL", InvestmentType.Stock, Money.From(60_000m, Currency.ARS), PurchasedOn, Now));
        investment.CurrentValue.ShouldBe(Money.From(56_000m, Currency.ARS));
    }

    [Fact]
    public void The_currency_of_an_imported_position_cannot_be_changed_by_hand()
    {
        var investment = Import();

        Should.Throw<DomainException>(() =>
            investment.Correct("GGAL", InvestmentType.Stock, Money.From(50_000m, Currency.USD), PurchasedOn, Now));
    }

    [Fact]
    public void The_type_and_purchase_date_of_an_imported_position_cannot_be_changed_by_hand()
    {
        var investment = Import();

        Should.Throw<DomainException>(() =>
            investment.Correct("GGAL", InvestmentType.Cedear, Money.From(50_000m, Currency.ARS), PurchasedOn, Now));
        Should.Throw<DomainException>(() =>
            investment.Correct("GGAL", InvestmentType.Stock, Money.From(50_000m, Currency.ARS), PurchasedOn.AddDays(-1), Now));
    }

    [Fact]
    public void Editing_an_imported_position_with_the_same_symbol_and_quantity_is_allowed()
    {
        var investment = Import();

        investment.Track("ggal", 10m, Now);

        investment.Symbol.ShouldBe("GGAL");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_position_needs_a_positive_quantity(decimal quantity)
    {
        var investment = Import();

        Should.Throw<DomainException>(() =>
            investment.UpdatePosition(quantity, Money.From(50_000m, Currency.ARS), Money.From(56_000m, Currency.ARS), Today, Now));
    }

    private static Investment Import(
        InvestmentSource source = InvestmentSource.Iol,
        string reference = "bcba:GGAL",
        DateOnly? purchasedOn = null) =>
        Investment.Import(
            EmployeeId,
            source,
            reference,
            "GGAL - Grupo Financiero Galicia",
            InvestmentType.Stock,
            "GGAL",
            10m,
            Money.From(50_000m, Currency.ARS),
            purchasedOn ?? PurchasedOn,
            Money.From(56_000m, Currency.ARS),
            Today,
            Now);
}
