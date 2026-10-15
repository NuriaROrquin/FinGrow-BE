namespace FinGrow.Application.UnitTests.Features.Integrations.Iol;

using FinGrow.Application.Features.Integrations.Iol.Sync;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;

public class IolPositionMapperTests
{
    [Theory]
    [InlineData("ACCIONES", InvestmentType.Stock)]
    [InlineData("CEDEARS", InvestmentType.Cedear)]
    [InlineData("TitulosPublicos", InvestmentType.Bond)]
    [InlineData("ObligacionesNegociables", InvestmentType.CorporateBond)]
    [InlineData("Letras", InvestmentType.TreasuryBill)]
    [InlineData("FondoComundeInversion", InvestmentType.MutualFund)]
    [InlineData("ETF", InvestmentType.Etf)]
    [InlineData("Titulos_Publicos", InvestmentType.Bond)]
    public void Each_supported_iol_type_maps_to_its_investment_type(string iolType, InvestmentType expected)
    {
        IolPositionMapper.ToHolding(Position(type: iolType))!.Type.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Opciones")]
    [InlineData("Cauciones")]
    [InlineData("Futuros")]
    [InlineData("")]
    public void Options_repos_and_unknown_types_are_not_imported(string iolType)
    {
        IolPositionMapper.ToHolding(Position(type: iolType)).ShouldBeNull();
    }

    [Theory]
    [InlineData("peso_Argentino", Currency.ARS)]
    [InlineData("dolar_Estadounidense", Currency.USD)]
    public void The_currency_of_the_title_is_the_currency_of_the_investment(string iolCurrency, Currency expected)
    {
        IolPositionMapper.ToHolding(Position(currency: iolCurrency))!.Currency.ShouldBe(expected);
    }

    [Fact]
    public void An_unknown_currency_is_not_imported()
    {
        IolPositionMapper.ToHolding(Position(currency: "real_Brasileño")).ShouldBeNull();
    }

    [Fact]
    public void The_capital_is_the_quantity_times_the_average_purchase_price_and_the_value_is_what_iol_values()
    {
        var holding = IolPositionMapper.ToHolding(Position(quantity: 10m, averagePrice: 5_012.5m, valued: 56_000m))!;

        holding.Quantity.ShouldBe(10m);
        holding.InvestedAmount.ShouldBe(50_125m);
        holding.ValuedAmount.ShouldBe(56_000m);
    }

    [Fact]
    public void The_reference_is_the_market_and_the_symbol()
    {
        var holding = IolPositionMapper.ToHolding(Position(symbol: " ggal ", market: "bCBA"))!;

        holding.ExternalReference.ShouldBe("bcba:GGAL");
        holding.Symbol.ShouldBe("ggal");
    }

    [Fact]
    public void The_name_joins_symbol_and_description()
    {
        IolPositionMapper.ToHolding(Position(symbol: "GGAL", description: "Grupo Financiero Galicia"))!
            .AssetName.ShouldBe("GGAL - Grupo Financiero Galicia");
    }

    [Fact]
    public void Without_description_the_name_is_the_symbol()
    {
        IolPositionMapper.ToHolding(Position(symbol: "GGAL", description: " "))!.AssetName.ShouldBe("GGAL");
    }

    [Fact]
    public void A_long_description_is_cut_to_the_name_limit()
    {
        IolPositionMapper.ToHolding(Position(description: new string('a', 300)))!
            .AssetName.Length.ShouldBe(Investment.MaxAssetNameLength);
    }

    [Theory]
    [InlineData(0, 5000)]
    [InlineData(10, 0)]
    [InlineData(0.0001, 0.0001)]
    public void A_position_without_quantity_or_cost_is_not_imported(decimal quantity, decimal averagePrice)
    {
        IolPositionMapper.ToHolding(Position(quantity: quantity, averagePrice: averagePrice)).ShouldBeNull();
    }

    [Fact]
    public void A_symbol_longer_than_an_investment_accepts_is_not_imported()
    {
        IolPositionMapper.ToHolding(Position(symbol: new string('X', Investment.MaxSymbolLength + 1))).ShouldBeNull();
    }

    [Fact]
    public void Parts_of_the_same_title_are_consolidated_into_one_holding()
    {
        var holdings = IolPositionMapper.Consolidate(new[]
        {
            IolPositionMapper.ToHolding(Position(quantity: 10m, averagePrice: 5000m, valued: 56_000m))!,
            IolPositionMapper.ToHolding(Position(quantity: 5m, averagePrice: 5200m, valued: 28_000m))!,
            IolPositionMapper.ToHolding(Position(symbol: "AAPL", type: "CEDEARS", quantity: 2m, averagePrice: 15_000m, valued: 31_000m))!,
        });

        holdings.Count.ShouldBe(2);
        var galicia = holdings.Single(holding => holding.ExternalReference == "bcba:GGAL");
        galicia.Quantity.ShouldBe(15m);
        galicia.InvestedAmount.ShouldBe(76_000m);
        galicia.ValuedAmount.ShouldBe(84_000m);
    }

    [Fact]
    public void The_first_purchase_of_each_symbol_is_its_earliest_completed_buy()
    {
        var dates = IolPositionMapper.FirstPurchaseDates(new[]
        {
            new IolOperation("GGAL", "Compra", new DateOnly(2026, 9, 10)),
            new IolOperation("ggal", "Compra", new DateOnly(2026, 8, 3)),
            new IolOperation("GGAL", "Venta", new DateOnly(2026, 7, 1)),
            new IolOperation("IOLPORA", "Suscripción FCI", new DateOnly(2026, 6, 2)),
            new IolOperation("AAPL", "Compra", null),
        });

        dates.Count.ShouldBe(2);
        dates["GGAL"].ShouldBe(new DateOnly(2026, 8, 3));
        dates["IOLPORA"].ShouldBe(new DateOnly(2026, 6, 2));
    }

    private static IolPosition Position(
        string symbol = "GGAL",
        string? description = "Grupo Financiero Galicia",
        string type = "ACCIONES",
        string market = "bCBA",
        string currency = "peso_Argentino",
        decimal quantity = 10m,
        decimal averagePrice = 5000m,
        decimal valued = 56_000m) =>
        new(symbol, description, type, market, currency, quantity, averagePrice, valued);
}
