namespace FinGrow.Application.UnitTests.Features.SecurityPrices;

using Common;
using DTOs;
using FinGrow.Application.Features.SecurityPrices.GetSecurityPrice;
using Fakes;
using Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

public class GetSecurityPriceHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateOnly LastFriday = new(2026, 10, 2);

    private readonly FakeMarketPriceProvider _byma = new();
    private readonly FakeMarketPriceProvider _funds = new() { Market = PriceMarket.MutualFund, Source = "ArgentinaDatos" };
    private readonly FakeMarketPriceProvider _crypto = new() { Market = PriceMarket.Crypto, Source = "CoinGecko" };
    private readonly FakeSecurityPriceRepository _stored = new();

    [Fact]
    public async Task A_symbol_with_a_live_price_is_quoted_with_the_price_byma_publishes_today()
    {
        _byma.Prices.Add(new MarketPrice("AL30", Currency.ARS, 839.40m));
        _stored.Prices.Add(SecurityPrice.Create(PriceMarket.Exchange, "AL30", Currency.ARS, 830m, LastFriday, "BYMA", Now.AddDays(-3)));

        var result = await HandleAsync("AL30", Currency.ARS, InvestmentType.Bond);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SecurityPriceResponse("AL30", null, Currency.ARS, 839.40m, Today, "BYMA"));
    }

    [Fact]
    public async Task The_symbol_is_looked_up_trimmed_and_in_upper_case()
    {
        _byma.Prices.Add(new MarketPrice("YPFD", Currency.ARS, 8305m));

        var result = await HandleAsync(" ypfd ", Currency.ARS, InvestmentType.Stock);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Symbol.ShouldBe("YPFD");
        result.Value.UnitPrice.ShouldBe(8305m);
    }

    [Fact]
    public async Task The_currency_picks_the_variant_and_the_peso_price_is_not_used_for_dollars()
    {
        _byma.Prices.Add(new MarketPrice("AL30", Currency.ARS, 839.40m));
        _byma.Prices.Add(new MarketPrice("AL30D", Currency.USD, 0.5397m));

        var dollars = await HandleAsync("AL30D", Currency.USD, InvestmentType.Bond);
        var wrongVariant = await HandleAsync("AL30", Currency.USD, InvestmentType.Bond);

        dollars.Value.UnitPrice.ShouldBe(0.5397m);
        wrongVariant.IsFailure.ShouldBeTrue();
        wrongVariant.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task The_same_symbol_is_priced_by_the_market_of_the_investment_type()
    {
        _byma.Prices.Add(new MarketPrice("COIN", Currency.USD, 9.80m));
        _crypto.Prices.Add(new MarketPrice("COIN", Currency.USD, 0.0031m, "Coin Token"));

        var cedear = await HandleAsync("COIN", Currency.USD, InvestmentType.Cedear);
        var crypto = await HandleAsync("COIN", Currency.USD, InvestmentType.Crypto);

        cedear.Value.UnitPrice.ShouldBe(9.80m);
        cedear.Value.Source.ShouldBe("BYMA");
        crypto.Value.UnitPrice.ShouldBe(0.0031m);
        crypto.Value.Name.ShouldBe("Coin Token");
        crypto.Value.Source.ShouldBe("CoinGecko");
    }

    [Fact]
    public async Task A_mutual_fund_is_found_by_name_regardless_of_case_and_currency_with_the_date_of_its_quote()
    {
        _funds.Prices.Add(new MarketPrice("1810 Ahorro", Currency.ARS, 240.424543m, PricedOn: LastFriday));

        var result = await HandleAsync("1810 AHORRO", Currency.USD, InvestmentType.MutualFund);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SecurityPriceResponse("1810 Ahorro", null, Currency.ARS, 240.424543m, LastFriday, "ArgentinaDatos"));
    }

    [Fact]
    public async Task When_byma_publishes_no_prices_like_on_a_weekend_the_last_stored_close_comes_with_its_date()
    {
        _stored.Prices.Add(SecurityPrice.Create(PriceMarket.Exchange, "AL30", Currency.ARS, 839.40m, LastFriday, "BYMA", Now.AddDays(-3)));

        var result = await HandleAsync("AL30", Currency.ARS, InvestmentType.Bond);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new SecurityPriceResponse("AL30", null, Currency.ARS, 839.40m, LastFriday, "BYMA"));
    }

    [Fact]
    public async Task A_stored_close_of_another_market_is_not_used()
    {
        _stored.Prices.Add(SecurityPrice.Create(PriceMarket.Exchange, "COIN", Currency.USD, 9.80m, LastFriday, "BYMA", Now.AddDays(-3)));

        var result = await HandleAsync("COIN", Currency.USD, InvestmentType.Crypto);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Description.ShouldContain("CoinGecko");
    }

    [Fact]
    public async Task When_byma_does_not_answer_the_last_stored_close_is_used()
    {
        _byma.Failure = new HttpRequestException("open.bymadata.com.ar no responde");
        _stored.Prices.Add(SecurityPrice.Create(PriceMarket.Exchange, "AL30", Currency.ARS, 839.40m, LastFriday, "BYMA", Now.AddDays(-3)));

        var result = await HandleAsync("AL30", Currency.ARS, InvestmentType.Bond);

        result.IsSuccess.ShouldBeTrue();
        result.Value.PricedOn.ShouldBe(LastFriday);
    }

    [Fact]
    public async Task A_slow_source_falls_back_to_the_stored_close_like_one_that_does_not_answer()
    {
        _crypto.Failure = new TaskCanceledException("CoinGecko tardo demasiado");
        _stored.Prices.Add(SecurityPrice.Create(PriceMarket.Crypto, "BTC", Currency.USD, 84000m, LastFriday, "CoinGecko", Now.AddDays(-3)));

        var result = await HandleAsync("BTC", Currency.USD, InvestmentType.Crypto);

        result.IsSuccess.ShouldBeTrue();
        result.Value.UnitPrice.ShouldBe(84000m);
    }

    [Fact]
    public async Task A_symbol_that_byma_does_not_have_and_was_never_stored_is_not_found_with_a_hint_about_the_variant()
    {
        _byma.Prices.Add(new MarketPrice("AL30", Currency.ARS, 839.40m));

        var result = await HandleAsync("XXXX", Currency.ARS, InvestmentType.Bond);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("SecurityPrice.NotFound");
        result.Error.Description.ShouldContain("XXXX");
        result.Error.Description.ShouldContain("AL30D");
    }

    [Fact]
    public async Task A_fund_that_is_not_published_is_not_found_with_a_hint_to_pick_it_from_the_list()
    {
        _funds.Prices.Add(new MarketPrice("1810 Ahorro", Currency.ARS, 240.42m));

        var result = await HandleAsync("Fondo inventado", Currency.ARS, InvestmentType.MutualFund);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Description.ShouldContain("lista de fondos");
    }

    [Fact]
    public async Task When_byma_does_not_answer_and_nothing_was_stored_the_price_is_unavailable()
    {
        _byma.Failure = new HttpRequestException("open.bymadata.com.ar no responde");

        var result = await HandleAsync("AL30", Currency.ARS, InvestmentType.Bond);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
        result.Error.Code.ShouldBe("SecurityPrice.Unavailable");
        result.Error.Description.ShouldContain("BYMA");
    }

    [Fact]
    public async Task A_type_without_a_price_source_is_rejected()
    {
        var result = await HandleAsync("PF", Currency.ARS, InvestmentType.FixedTermDeposit);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    private Task<Result<SecurityPriceResponse>> HandleAsync(string symbol, Currency currency, InvestmentType type) =>
        new GetSecurityPriceHandler(
                new[] { _byma, _funds, _crypto },
                _stored,
                new FakeDateTimeProvider(Now),
                NullLogger<GetSecurityPriceHandler>.Instance)
            .Handle(new GetSecurityPriceQuery(symbol, currency, type), CancellationToken.None);
}
