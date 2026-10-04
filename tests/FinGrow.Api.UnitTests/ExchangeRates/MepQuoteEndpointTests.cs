namespace FinGrow.Api.UnitTests.ExchangeRates;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class MepQuoteEndpointTests
{
    private const string DolarApiQuote =
        """{"moneda":"USD","casa":"bolsa","nombre":"Bolsa","compra":1544.3,"venta":1557.3,"fechaActualizacion":"2026-09-27T14:57:00.000Z"}""";

    private static readonly Uri MepEndpoint = new("/api/exchange-rates/mep", UriKind.Relative);

    [Fact]
    public async Task The_mep_quote_comes_from_dolarapi_as_dollars_to_pesos()
    {
        using var factory = new DolarApiWebApplicationFactory(HttpStatusCode.OK, DolarApiQuote);
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(MepEndpoint);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("baseCurrency").GetString().ShouldBe("USD");
        body.GetProperty("quoteCurrency").GetString().ShouldBe("ARS");
        body.GetProperty("buy").GetDecimal().ShouldBe(1544.3m);
        body.GetProperty("sell").GetDecimal().ShouldBe(1557.3m);
        body.GetProperty("updatedAt").GetDateTimeOffset().ShouldBe(new DateTimeOffset(2026, 9, 27, 14, 57, 0, TimeSpan.Zero));
        body.GetProperty("source").GetString().ShouldBe("DolarApi");
        factory.DolarApi.Requests.ShouldHaveSingleItem().AbsolutePath.ShouldBe("/v1/dolares/bolsa");
    }

    [Fact]
    public async Task A_second_request_reuses_the_quote_without_calling_dolarapi_again()
    {
        using var factory = new DolarApiWebApplicationFactory(HttpStatusCode.OK, DolarApiQuote);
        var client = AuthenticatedClient(factory);

        var first = await client.GetAsync(MepEndpoint);
        var second = await client.GetAsync(MepEndpoint);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.DolarApi.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task When_dolarapi_fails_the_endpoint_answers_503_with_a_readable_message()
    {
        using var factory = new DolarApiWebApplicationFactory(HttpStatusCode.InternalServerError, "{}");
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(MepEndpoint);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("title").GetString().ShouldBe("ExchangeRate.MepUnavailable");
        body.GetProperty("detail").GetString().ShouldNotBeNull().ShouldContain("dolar MEP");
    }

    [Theory]
    [InlineData("""{"moneda":"USD","casa":"bolsa"}""")]
    [InlineData("""{"compra":null,"venta":1557.3,"fechaActualizacion":"2026-09-27T14:57:00.000Z"}""")]
    [InlineData("no es json")]
    public async Task A_quote_that_cannot_be_used_is_treated_as_unavailable_and_not_reused(string dolarApiBody)
    {
        using var factory = new DolarApiWebApplicationFactory(HttpStatusCode.OK, dolarApiBody);
        var client = AuthenticatedClient(factory);

        var first = await client.GetAsync(MepEndpoint);
        var second = await client.GetAsync(MepEndpoint);

        first.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        second.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        factory.DolarApi.Requests.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Without_a_session_the_quote_is_not_served()
    {
        using var factory = new DolarApiWebApplicationFactory(HttpStatusCode.OK, DolarApiQuote);
        var client = factory.CreateClient();

        var response = await client.GetAsync(MepEndpoint);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.DolarApi.Requests.ShouldBeEmpty();
    }

    private static HttpClient AuthenticatedClient(DolarApiWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(Guid.CreateVersion7(), Guid.CreateVersion7(), Rol.Empleado, "Ana Gomez");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class StubDolarApiHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class DolarApiWebApplicationFactory(HttpStatusCode status, string body) : WebApplicationFactory<Program>
    {
        public StubDolarApiHandler DolarApi { get; } = new(status, body);

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
                services.AddHttpClient(nameof(IExchangeRateProvider))
                    .ConfigurePrimaryHttpMessageHandler(() => DolarApi));
        }
    }
}
