namespace FinGrow.Api.UnitTests.Investments;

using System.Net;
using System.Web;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Infrastructure.Integrations.CoinGecko;

public class CoinGeckoCryptoPriceProviderTests
{
    private const string DollarMarkets = """
        [{"id":"bitcoin","symbol":"btc","name":"Bitcoin","current_price":84805},
         {"id":"figure-heloc","symbol":"FIGR_HELOC","name":"Figure Heloc","current_price":1.02},
         {"id":"bitcoin-clone","symbol":"btc","name":"Bitcoin Clone","current_price":0.1},
         {"id":"shiba-inu","symbol":"shib","name":"Shiba Inu","current_price":2.40527e-07},
         {"id":"dead-coin","symbol":"dead","name":"Dead Coin","current_price":null}]
        """;

    private const string PesoMarkets = """
        [{"id":"bitcoin","symbol":"btc","name":"Bitcoin","current_price":129322758}]
        """;

    private static readonly string[] QuotedCurrencies = { "usd", "ars" };

    [Fact]
    public async Task The_top_coins_are_read_in_dollars_and_pesos_by_upper_case_symbol()
    {
        using var factory = Factory(HealthySource);

        var prices = await factory.Provider(PriceMarket.Crypto).GetClosingPricesAsync();

        prices.ShouldBe(
            new[]
            {
                new MarketPrice("BTC", Currency.USD, 84805m, "Bitcoin"),
                new MarketPrice("SHIB", Currency.USD, 0.000000240527m, "Shiba Inu"),
                new MarketPrice("BTC", Currency.ARS, 129322758m, "Bitcoin"),
            },
            ignoreOrder: true);
    }

    [Fact]
    public async Task Each_currency_asks_for_the_coins_ordered_by_market_cap()
    {
        using var factory = Factory(HealthySource);

        await factory.Provider(PriceMarket.Crypto).GetClosingPricesAsync();

        var queries = factory.Source.Requests.Select(request => HttpUtility.ParseQueryString(request.Uri.Query)).ToList();
        factory.Source.Requests.ShouldAllBe(request => request.Uri.AbsolutePath == "/api/v3/coins/markets");
        queries.Select(query => query["vs_currency"]).ShouldBe(QuotedCurrencies, ignoreOrder: true);
        queries.ShouldAllBe(query => query["order"] == "market_cap_desc" && query["per_page"] == "250");
    }

    [Fact]
    public async Task Without_an_api_key_no_key_header_is_sent()
    {
        using var factory = Factory(HealthySource);

        await factory.Provider(PriceMarket.Crypto).GetClosingPricesAsync();

        factory.Source.Requests.ShouldAllBe(request => !request.Headers.ContainsKey(CoinGeckoOptions.ApiKeyHeader));
    }

    [Fact]
    public async Task A_configured_api_key_travels_in_the_demo_key_header()
    {
        using var factory = Factory(HealthySource, new Dictionary<string, string?> { ["CoinGecko:ApiKey"] = "CG-demo-key" });

        await factory.Provider(PriceMarket.Crypto).GetClosingPricesAsync();

        factory.Source.Requests.ShouldAllBe(request => request.Headers[CoinGeckoOptions.ApiKeyHeader] == "CG-demo-key");
    }

    [Fact]
    public async Task A_second_download_within_the_cache_window_does_not_call_coingecko_again()
    {
        using var factory = Factory(HealthySource);

        await factory.Provider(PriceMarket.Crypto).GetClosingPricesAsync();
        await factory.Provider(PriceMarket.Crypto).GetClosingPricesAsync();

        factory.Source.Requests.Count.ShouldBe(QuotedCurrencies.Length);
    }

    [Fact]
    public async Task A_rate_limited_answer_is_reported_as_a_source_failure()
    {
        using var factory = Factory(_ => (HttpStatusCode.TooManyRequests, """{"status":{"error_code":429}}"""));

        await Should.ThrowAsync<HttpRequestException>(() => factory.Provider(PriceMarket.Crypto).GetClosingPricesAsync());
    }

    private static (HttpStatusCode Status, string Body) HealthySource(Uri uri) =>
        HttpUtility.ParseQueryString(uri.Query)["vs_currency"] switch
        {
            "usd" => (HttpStatusCode.OK, DollarMarkets),
            "ars" => (HttpStatusCode.OK, PesoMarkets),
            _ => (HttpStatusCode.OK, "[]"),
        };

    private static PriceSourceWebApplicationFactory Factory(
        Func<Uri, (HttpStatusCode Status, string Body)> respond,
        IReadOnlyDictionary<string, string?>? settings = null) =>
        new(CoinGeckoOptions.HttpClientName, new StubPriceSource(respond), settings);
}
