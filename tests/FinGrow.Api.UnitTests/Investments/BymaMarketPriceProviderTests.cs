namespace FinGrow.Api.UnitTests.Investments;

using System.Net;
using System.Text;
using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class BymaMarketPriceProviderTests
{
    private static readonly string[] ExpectedRequests =
    {
        "leading-equity:1",
        "general-equity:1",
        "general-equity:2",
        "cedears:1",
        "public-bonds:1",
        "negociable-obligations:1",
        "lebacs:1",
    };

    private const string LeadingEquity = """
        {"content":{"page_number":1,"page_count":1,"page_size":189,"total_elements_count":4},"data":[
          {"symbol":"YPFD","settlementType":"2","denominationCcy":"ARS","closingPrice":8305,"previousClosingPrice":8430},
          {"symbol":"GGAL","settlementType":"2","denominationCcy":"ARS","closingPrice":0,"previousClosingPrice":6000},
          {"symbol":"YPFD","settlementType":"1","denominationCcy":"ARS","closingPrice":9999,"previousClosingPrice":9999},
          {"symbol":"XYZ","settlementType":"2","denominationCcy":"EXT","closingPrice":10,"previousClosingPrice":10}]}
        """;

    private const string GeneralEquityFirstPage = """
        {"content":{"page_number":1,"page_count":2},"data":[{"symbol":"ALUA","settlementType":"2","denominationCcy":"ARS","closingPrice":700.5,"previousClosingPrice":690}]}
        """;

    private const string GeneralEquitySecondPage = """
        {"content":{"page_number":2,"page_count":2},"data":[{"symbol":"BYMA","settlementType":"2","denominationCcy":"ARS","closingPrice":300,"previousClosingPrice":310}]}
        """;

    private const string Cedears = """
        [{"symbol":"SPY","settlementType":"2","denominationCcy":"ARS","closingPrice":20700.0,"previousClosingPrice":20860.0},
         {"symbol":"SPYD","settlementType":"2","denominationCcy":"USD","closingPrice":13.37,"previousClosingPrice":13.45},
         {"symbol":"SPYC","settlementType":"2","denominationCcy":"EXT","closingPrice":12.79,"previousClosingPrice":13.03}]
        """;

    private const string PublicBonds = """
        {"content":{"page_number":1,"page_count":1},"data":[
          {"symbol":"AL30","settlementType":"2","denominationCcy":"ARS","closingPrice":83940,"previousClosingPrice":84130,"maturityDate":"2030-07-09"},
          {"symbol":"AL30D","settlementType":"2","denominationCcy":"USD","closingPrice":53.97,"previousClosingPrice":54.19}]}
        """;

    private const string TreasuryBills = """
        {"content":{"page_number":1,"page_count":1},"data":[
          {"symbol":"S30N6","settlementType":"2","denominationCcy":"ARS","closingPrice":110.55,"previousClosingPrice":110.2},
          {"symbol":"S30N6.SB","settlementType":"2","denominationCcy":"ARS","closingPrice":0,"previousClosingPrice":0}]}
        """;

    [Fact]
    public async Task Every_panel_is_read_with_next_day_settlement_and_bonds_are_priced_per_nominal()
    {
        using var factory = new BymaWebApplicationFactory(HealthyByma);

        var prices = await factory.Services.GetRequiredService<IMarketPriceProvider>().GetClosingPricesAsync();

        prices.ShouldBe(
            new[]
            {
                new MarketPrice("YPFD", Currency.ARS, 8305m),
                new MarketPrice("GGAL", Currency.ARS, 6000m),
                new MarketPrice("ALUA", Currency.ARS, 700.5m),
                new MarketPrice("BYMA", Currency.ARS, 300m),
                new MarketPrice("SPY", Currency.ARS, 20700m),
                new MarketPrice("SPYD", Currency.USD, 13.37m),
                new MarketPrice("AL30", Currency.ARS, 839.40m),
                new MarketPrice("AL30D", Currency.USD, 0.5397m),
                new MarketPrice("S30N6", Currency.ARS, 1.1055m),
            },
            ignoreOrder: true);
    }

    [Fact]
    public async Task Paginated_panels_are_read_until_the_last_page_and_every_request_asks_for_next_day_settlement()
    {
        using var factory = new BymaWebApplicationFactory(HealthyByma);

        await factory.Services.GetRequiredService<IMarketPriceProvider>().GetClosingPricesAsync();

        factory.Byma.Requests.Select(request => $"{request.Panel}:{request.PageNumber}").ShouldBe(ExpectedRequests);
        factory.Byma.Requests.ShouldAllBe(request => request.NextDay && !request.SameDay && !request.TwoDays);
    }

    [Fact]
    public async Task A_panel_that_fails_makes_the_whole_download_fail()
    {
        using var factory = new BymaWebApplicationFactory((panel, _) =>
            panel == "cedears" ? (HttpStatusCode.InternalServerError, "{}") : HealthyByma(panel, 1));

        await Should.ThrowAsync<HttpRequestException>(() =>
            factory.Services.GetRequiredService<IMarketPriceProvider>().GetClosingPricesAsync());
    }

    [Fact]
    public async Task A_panel_that_cannot_be_read_is_reported_as_a_source_failure()
    {
        using var factory = new BymaWebApplicationFactory((panel, page) =>
            panel == "public-bonds" ? (HttpStatusCode.OK, "<html>mantenimiento</html>") : HealthyByma(panel, page));

        var exception = await Should.ThrowAsync<HttpRequestException>(() =>
            factory.Services.GetRequiredService<IMarketPriceProvider>().GetClosingPricesAsync());

        exception.Message.ShouldContain("public-bonds");
    }

    [Fact]
    public async Task A_second_download_within_the_cache_window_reuses_the_prices_without_calling_byma_again()
    {
        using var factory = new BymaWebApplicationFactory(HealthyByma);

        var first = await factory.Services.GetRequiredService<IMarketPriceProvider>().GetClosingPricesAsync();
        var second = await factory.Services.GetRequiredService<IMarketPriceProvider>().GetClosingPricesAsync();

        second.ShouldBe(first);
        factory.Byma.Requests.Count.ShouldBe(ExpectedRequests.Length);
    }

    [Fact]
    public async Task A_failed_download_is_not_reused_and_the_next_one_calls_byma_again()
    {
        var bymaIsDown = true;
        using var factory = new BymaWebApplicationFactory((panel, page) =>
            bymaIsDown ? (HttpStatusCode.InternalServerError, "{}") : HealthyByma(panel, page));

        await Should.ThrowAsync<HttpRequestException>(() =>
            factory.Services.GetRequiredService<IMarketPriceProvider>().GetClosingPricesAsync());

        bymaIsDown = false;
        var prices = await factory.Services.GetRequiredService<IMarketPriceProvider>().GetClosingPricesAsync();

        prices.ShouldContain(new MarketPrice("AL30", Currency.ARS, 839.40m));
    }

    private static (HttpStatusCode Status, string Body) HealthyByma(string panel, int page) => panel switch
    {
        "leading-equity" => (HttpStatusCode.OK, LeadingEquity),
        "general-equity" => (HttpStatusCode.OK, page == 1 ? GeneralEquityFirstPage : GeneralEquitySecondPage),
        "cedears" => (HttpStatusCode.OK, Cedears),
        "public-bonds" => (HttpStatusCode.OK, PublicBonds),
        "lebacs" => (HttpStatusCode.OK, TreasuryBills),
        _ => (HttpStatusCode.OK, "[]"),
    };

    private sealed record BymaRequest(string Panel, int PageNumber, bool SameDay, bool NextDay, bool TwoDays);

    private sealed class StubBymaHandler(Func<string, int, (HttpStatusCode Status, string Body)> respond) : HttpMessageHandler
    {
        public List<BymaRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var panel = request.RequestUri!.Segments[^1];
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var root = body.RootElement;
            var pageNumber = root.GetProperty("page_number").GetInt32();

            Requests.Add(new BymaRequest(
                panel,
                pageNumber,
                root.GetProperty("T0").GetBoolean(),
                root.GetProperty("T1").GetBoolean(),
                root.GetProperty("T2").GetBoolean()));

            var (status, content) = respond(panel, pageNumber);

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class BymaWebApplicationFactory(Func<string, int, (HttpStatusCode Status, string Body)> respond)
        : WebApplicationFactory<Program>
    {
        public StubBymaHandler Byma { get; } = new(respond);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
                    ["AiService:ApiKey"] = "unit-test-ai-api-key",
                    ["Twilio:AccountSid"] = "ACunit-test",
                    ["Twilio:AuthToken"] = "unit-test-twilio-auth-token",
                    ["Telegram:BotToken"] = "unit-test-telegram-bot-token",
                    ["Telegram:WebhookSecret"] = "unit-test-telegram-secret",
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key-1234",
                }));

            builder.ConfigureTestServices(services =>
                services.AddHttpClient(nameof(IMarketPriceProvider))
                    .ConfigurePrimaryHttpMessageHandler(() => Byma));
        }
    }
}
