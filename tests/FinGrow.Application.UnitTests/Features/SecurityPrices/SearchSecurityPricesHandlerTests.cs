namespace FinGrow.Application.UnitTests.Features.SecurityPrices;

using Common;
using DTOs;
using FinGrow.Application.Features.SecurityPrices.SearchSecurityPrices;
using Fakes;
using Interfaces;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

public class SearchSecurityPricesHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly LastFriday = new(2026, 10, 2);

    private static readonly string[] BalanzFundsInOrder =
    {
        "Balanz Ahorro - Clase A",
        "Balanz Capital Money Market - Clase A",
        "Superfondo Balanz - Clase B",
    };

    private readonly FakeMarketPriceProvider _byma = new();
    private readonly FakeMarketPriceProvider _funds = new() { Market = PriceMarket.MutualFund, Source = "ArgentinaDatos" };
    private readonly FakeMarketPriceProvider _crypto = new() { Market = PriceMarket.Crypto, Source = "CoinGecko" };

    [Fact]
    public async Task Funds_are_found_by_part_of_their_name_without_minding_accents_or_case()
    {
        _funds.Prices.Add(new MarketPrice("1822 Raices Ahorro Dólares - Clase A", Currency.USD, 1.004402m, PricedOn: LastFriday));
        _funds.Prices.Add(new MarketPrice("Balanz Ahorro en Dolares - Clase A", Currency.USD, 1.2m, PricedOn: LastFriday));
        _funds.Prices.Add(new MarketPrice("1810 Ahorro", Currency.ARS, 240.42m, PricedOn: LastFriday));

        var result = await SearchAsync(InvestmentType.MutualFund, "ahorro dolares");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldHaveSingleItem().Symbol.ShouldBe("1822 Raices Ahorro Dólares - Clase A");
    }

    [Fact]
    public async Task Matches_that_start_with_the_text_come_first_and_shorter_names_before_longer_ones()
    {
        _funds.Prices.Add(new MarketPrice("Fima Ahorro Pesos - Clase A", Currency.ARS, 1m));
        _funds.Prices.Add(new MarketPrice("Balanz Capital Money Market - Clase A", Currency.ARS, 1m));
        _funds.Prices.Add(new MarketPrice("Balanz Ahorro - Clase A", Currency.ARS, 1m));
        _funds.Prices.Add(new MarketPrice("Superfondo Balanz - Clase B", Currency.ARS, 1m));

        var result = await SearchAsync(InvestmentType.MutualFund, "balanz");

        result.Value.Select(price => price.Symbol).ShouldBe(BalanzFundsInOrder);
    }

    [Fact]
    public async Task Cryptos_are_found_by_symbol_or_name_in_the_requested_currency()
    {
        _crypto.Prices.Add(new MarketPrice("BTC", Currency.USD, 84805m, "Bitcoin"));
        _crypto.Prices.Add(new MarketPrice("BTC", Currency.ARS, 129322758m, "Bitcoin"));
        _crypto.Prices.Add(new MarketPrice("WBTC", Currency.USD, 84700m, "Wrapped Bitcoin"));
        _crypto.Prices.Add(new MarketPrice("ETH", Currency.USD, 2692.87m, "Ethereum"));

        var result = await SearchAsync(InvestmentType.Crypto, "bitcoin", Currency.USD);

        result.Value.ShouldBe(new[]
        {
            new SecurityPriceResponse("BTC", "Bitcoin", Currency.USD, 84805m, new DateOnly(2026, 10, 5), "CoinGecko"),
            new SecurityPriceResponse("WBTC", "Wrapped Bitcoin", Currency.USD, 84700m, new DateOnly(2026, 10, 5), "CoinGecko"),
        });
    }

    [Fact]
    public async Task The_search_only_looks_in_the_market_of_the_investment_type()
    {
        _byma.Prices.Add(new MarketPrice("ETHA", Currency.USD, 20m));
        _crypto.Prices.Add(new MarketPrice("ETH", Currency.USD, 2692.87m, "Ethereum"));

        var result = await SearchAsync(InvestmentType.Cedear, "eth");

        result.Value.ShouldHaveSingleItem().Symbol.ShouldBe("ETHA");
    }

    [Fact]
    public async Task No_more_than_twenty_results_are_returned()
    {
        _funds.Prices.AddRange(Enumerable.Range(1, 30).Select(number => new MarketPrice($"Fondo {number:00}", Currency.ARS, 1m)));

        var result = await SearchAsync(InvestmentType.MutualFund, "fondo");

        result.Value.Count.ShouldBe(SearchSecurityPricesHandler.MaxResults);
    }

    [Fact]
    public async Task A_source_that_does_not_answer_is_reported_as_unavailable()
    {
        _funds.Failure = new HttpRequestException("api.argentinadatos.com no responde");

        var result = await SearchAsync(InvestmentType.MutualFund, "balanz");

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Unavailable);
        result.Error.Description.ShouldContain("ArgentinaDatos");
    }

    private Task<Result<IReadOnlyList<SecurityPriceResponse>>> SearchAsync(
        InvestmentType type,
        string query,
        Currency? currency = null) =>
        new SearchSecurityPricesHandler(
                new[] { _byma, _funds, _crypto },
                new FakeDateTimeProvider(Now),
                NullLogger<SearchSecurityPricesHandler>.Instance)
            .Handle(new SearchSecurityPricesQuery(type, query, currency), CancellationToken.None);
}
