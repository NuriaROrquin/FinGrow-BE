namespace FinGrow.Api.UnitTests.SecurityPrices;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Infrastructure.Integrations.Byma;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class SecurityPriceEndpointTests
{
    private const string PublicBonds = """
        {"content":{"page_number":1,"page_count":1},"data":[
          {"symbol":"AL30","settlementType":"2","denominationCcy":"ARS","closingPrice":83940,"previousClosingPrice":84130},
          {"symbol":"AL30D","settlementType":"2","denominationCcy":"USD","closingPrice":53.97,"previousClosingPrice":54.19}]}
        """;

    private const string WeekendPublicBonds = """
        {"content":{"page_number":1,"page_count":1},"data":[
          {"symbol":"AL30","settlementType":"2","denominationCcy":"ARS","closingPrice":0,"previousClosingPrice":0}]}
        """;

    private static readonly DateOnly LastFriday = new(2026, 10, 2);

    private static readonly string[] SearchedBonds = { "AL30", "AL30D" };

    [Fact]
    public async Task A_bond_is_quoted_per_nominal_with_the_price_byma_publishes_today()
    {
        using var factory = new SecurityPricesWebApplicationFactory(Byma(PublicBonds));
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri("/api/security-prices?symbol=al30d&currency=USD&type=Bond", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("symbol").GetString().ShouldBe("AL30D");
        body.GetProperty("currency").GetString().ShouldBe("USD");
        body.GetProperty("unitPrice").GetDecimal().ShouldBe(0.5397m);
        body.GetProperty("pricedOn").GetString().ShouldBe(ArgentinaTime.DateOf(DateTimeOffset.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        body.GetProperty("source").GetString().ShouldBe("BYMA");
    }

    [Fact]
    public async Task Looking_up_several_symbols_downloads_byma_only_once()
    {
        using var factory = new SecurityPricesWebApplicationFactory(Byma(PublicBonds));
        var client = AuthenticatedClient(factory);

        await client.GetAsync(new Uri("/api/security-prices?symbol=AL30&currency=ARS&type=Bond", UriKind.Relative));
        var downloaded = factory.Byma.Requests;
        await client.GetAsync(new Uri("/api/security-prices?symbol=AL30D&currency=USD&type=Bond", UriKind.Relative));

        downloaded.ShouldBeGreaterThan(0);
        factory.Byma.Requests.ShouldBe(downloaded);
    }

    [Fact]
    public async Task On_a_weekend_byma_comes_in_zero_and_the_last_stored_close_is_served_with_its_date()
    {
        using var factory = new SecurityPricesWebApplicationFactory(Byma(WeekendPublicBonds));
        factory.StoredPrices.Prices.Add(SecurityPrice.Create(PriceMarket.Exchange, "AL30", Currency.ARS, 839.40m, LastFriday, "BYMA", DateTimeOffset.UtcNow));
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri("/api/security-prices?symbol=AL30&currency=ARS&type=Bond", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("unitPrice").GetDecimal().ShouldBe(839.40m);
        body.GetProperty("pricedOn").GetString().ShouldBe("2026-10-02");
    }

    [Fact]
    public async Task A_symbol_byma_does_not_have_in_that_currency_answers_404_with_a_readable_message()
    {
        using var factory = new SecurityPricesWebApplicationFactory(Byma(PublicBonds));
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri("/api/security-prices?symbol=AL30&currency=USD&type=Bond", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().ShouldBe("SecurityPrice.NotFound");
        body.GetProperty("detail").GetString().ShouldNotBeNull().ShouldContain("AL30D");
    }

    [Fact]
    public async Task When_byma_fails_and_nothing_was_stored_the_endpoint_answers_503()
    {
        using var factory = new SecurityPricesWebApplicationFactory((_, _) => (HttpStatusCode.InternalServerError, "{}"));
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri("/api/security-prices?symbol=AL30&currency=ARS&type=Bond", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().ShouldBe("SecurityPrice.Unavailable");
    }

    [Theory]
    [InlineData("/api/security-prices?symbol=AL30&currency=EUR&type=Bond")]
    [InlineData("/api/security-prices?symbol=AL30&type=Bond")]
    [InlineData("/api/security-prices?symbol=AL30&currency=ARS")]
    [InlineData("/api/security-prices?symbol=AL30&currency=ARS&type=FixedTermDeposit")]
    [InlineData("/api/security-prices?currency=ARS&type=Bond")]
    [InlineData("/api/security-prices?symbol=AL30&currency=XYZ&type=Bond")]
    [InlineData("/api/security-prices?symbol=AL-30&currency=ARS&type=Bond")]
    public async Task A_currency_byma_does_not_quote_or_an_invalid_symbol_is_rejected_without_calling_byma(string path)
    {
        using var factory = new SecurityPricesWebApplicationFactory(Byma(PublicBonds));
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Byma.Requests.ShouldBe(0);
    }

    [Fact]
    public async Task The_search_returns_the_symbols_of_the_market_that_contain_the_text()
    {
        using var factory = new SecurityPricesWebApplicationFactory(Byma(PublicBonds));
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri("/api/security-prices/search?type=Bond&query=al30", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.EnumerateArray().Select(price => price.GetProperty("symbol").GetString()).ShouldBe(SearchedBonds);
    }

    [Fact]
    public async Task A_search_with_a_single_character_is_rejected_without_calling_byma()
    {
        using var factory = new SecurityPricesWebApplicationFactory(Byma(PublicBonds));
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri("/api/security-prices/search?type=Bond&query=a", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Byma.Requests.ShouldBe(0);
    }

    [Fact]
    public async Task Without_a_session_the_price_is_not_served()
    {
        using var factory = new SecurityPricesWebApplicationFactory(Byma(PublicBonds));
        var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/security-prices?symbol=AL30&currency=ARS&type=Bond", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Byma.Requests.ShouldBe(0);
    }

    private static Func<string, int, (HttpStatusCode Status, string Body)> Byma(string publicBonds) =>
        (panel, _) => (HttpStatusCode.OK, panel == "public-bonds" ? publicBonds : "[]");

    private static HttpClient AuthenticatedClient(SecurityPricesWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(Guid.CreateVersion7(), Guid.CreateVersion7(), Rol.Empleado, "Ana Gomez");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class StubBymaHandler(Func<string, int, (HttpStatusCode Status, string Body)> respond) : HttpMessageHandler
    {
        private int _requests;

        public int Requests => _requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);

            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var (status, content) = respond(request.RequestUri!.Segments[^1], body.RootElement.GetProperty("page_number").GetInt32());

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class InMemorySecurityPriceRepository : ISecurityPriceRepository
    {
        public List<SecurityPrice> Prices { get; } = new();

        public Task<SecurityPrice?> FindAsync(
            PriceMarket market,
            string symbol,
            Currency currency,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Prices.FirstOrDefault(price =>
                price.Market == market && price.Symbol == symbol && price.Currency == currency));

        public Task<IReadOnlyList<SecurityPrice>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SecurityPrice>>(Prices.ToList());

        public void AddRange(IEnumerable<SecurityPrice> prices) => Prices.AddRange(prices);
    }

    private sealed class SecurityPricesWebApplicationFactory(Func<string, int, (HttpStatusCode Status, string Body)> respond)
        : WebApplicationFactory<Program>
    {
        public StubBymaHandler Byma { get; } = new(respond);

        public InMemorySecurityPriceRepository StoredPrices { get; } = new();

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
            {
                services.AddHttpClient(BymaOptions.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => Byma);

                services.RemoveAll<ISecurityPriceRepository>();
                services.AddSingleton<ISecurityPriceRepository>(StoredPrices);
            });
        }
    }
}
