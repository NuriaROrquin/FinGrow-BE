namespace FinGrow.Api.UnitTests.Investments;

using System.Net;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using FinGrow.Infrastructure.Integrations.ArgentinaDatos;

public class ArgentinaDatosFundPriceProviderTests
{
    private const string MoneyMarketFunds = """
        [{"fondo":"1810 Ahorro","horizonte":"corto","fecha":"2026-10-02","vcp":240424.543,"ccp":6739876122,"patrimonio":1620431636657.14},
         {"fondo":"1822 Raices Ahorro Dólares - Clase A","horizonte":"corto","fecha":"2026-10-02","vcp":1004.402,"ccp":46001739.12,"patrimonio":46204257.08},
         {"fondo":"Fondo Liquidado - Clase A","horizonte":"corto","fecha":"2020-05-28","vcp":1500,"ccp":1,"patrimonio":1},
         {"fondo":"Sin Valor - Clase A","horizonte":"corto","fecha":"2026-10-02","vcp":null,"ccp":null,"patrimonio":null}]
        """;

    private const string FixedIncomeFunds = """
        [{"fondo":"Balanz Dólar Linked - Clase A","horizonte":"medio","fecha":"2026-10-01","vcp":1234.5,"ccp":1,"patrimonio":1},
         {"fondo":"1810 Ahorro","horizonte":"corto","fecha":"2026-09-30","vcp":239000,"ccp":1,"patrimonio":1}]
        """;

    [Fact]
    public async Task Every_category_is_read_and_the_quote_is_per_cuotaparte_with_its_date()
    {
        using var factory = Factory(HealthySource);

        var prices = await factory.Provider(PriceMarket.MutualFund).GetClosingPricesAsync();

        prices.ShouldBe(
            new[]
            {
                new MarketPrice("1810 Ahorro", Currency.ARS, 240.424543m, PricedOn: new DateOnly(2026, 10, 2)),
                new MarketPrice("1822 Raices Ahorro Dólares - Clase A", Currency.USD, 1.004402m, PricedOn: new DateOnly(2026, 10, 2)),
                new MarketPrice("Balanz Dólar Linked - Clase A", Currency.ARS, 1.2345m, PricedOn: new DateOnly(2026, 10, 1)),
            },
            ignoreOrder: true);
        factory.Source.Requests.Select(request => request.Uri.AbsolutePath).ShouldBe(
            ArgentinaDatosFundPriceProvider.Categories.Select(category => $"/v1/finanzas/fci/{category}/ultimo"),
            ignoreOrder: true);
        factory.Source.Requests.ShouldAllBe(request => request.Headers["User-Agent"].StartsWith("FinGrow", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("1822 Raices Ahorro Dólares - Clase A", Currency.USD)]
    [InlineData("Allaria Dolar Ahorro - Clase A", Currency.USD)]
    [InlineData("Fima Renta en USD - Clase B", Currency.USD)]
    [InlineData("Balanz Dólar Linked - Clase A", Currency.ARS)]
    [InlineData("1810 Ahorro", Currency.ARS)]
    public void The_currency_of_a_fund_is_guessed_from_its_name(string fund, Currency currency)
    {
        ArgentinaDatosFundPriceProvider.CurrencyOf(fund).ShouldBe(currency);
    }

    [Fact]
    public async Task A_second_download_within_the_cache_window_does_not_call_the_source_again()
    {
        using var factory = Factory(HealthySource);

        await factory.Provider(PriceMarket.MutualFund).GetClosingPricesAsync();
        await factory.Provider(PriceMarket.MutualFund).GetClosingPricesAsync();

        factory.Source.Requests.Count.ShouldBe(ArgentinaDatosFundPriceProvider.Categories.Count);
    }

    [Fact]
    public async Task A_category_that_fails_makes_the_whole_download_fail()
    {
        using var factory = Factory(uri =>
            uri.AbsolutePath.Contains("rentaVariable", StringComparison.Ordinal)
                ? (HttpStatusCode.InternalServerError, "{}")
                : HealthySource(uri));

        await Should.ThrowAsync<HttpRequestException>(() => factory.Provider(PriceMarket.MutualFund).GetClosingPricesAsync());
    }

    [Fact]
    public async Task A_category_that_cannot_be_read_is_reported_as_a_source_failure()
    {
        using var factory = Factory(uri =>
            uri.AbsolutePath.Contains("rentaMixta", StringComparison.Ordinal)
                ? (HttpStatusCode.OK, "<html>mantenimiento</html>")
                : HealthySource(uri));

        var exception = await Should.ThrowAsync<HttpRequestException>(() =>
            factory.Provider(PriceMarket.MutualFund).GetClosingPricesAsync());

        exception.Message.ShouldContain("rentaMixta");
    }

    private static (HttpStatusCode Status, string Body) HealthySource(Uri uri) =>
        uri.AbsolutePath switch
        {
            "/v1/finanzas/fci/mercadoDinero/ultimo" => (HttpStatusCode.OK, MoneyMarketFunds),
            "/v1/finanzas/fci/rentaFija/ultimo" => (HttpStatusCode.OK, FixedIncomeFunds),
            _ => (HttpStatusCode.OK, "[]"),
        };

    private static PriceSourceWebApplicationFactory Factory(Func<Uri, (HttpStatusCode Status, string Body)> respond) =>
        new(ArgentinaDatosOptions.HttpClientName, new StubPriceSource(respond));
}
