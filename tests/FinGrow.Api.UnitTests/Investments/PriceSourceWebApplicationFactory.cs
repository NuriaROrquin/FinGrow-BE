namespace FinGrow.Api.UnitTests.Investments;

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

internal sealed record RecordedRequest(Uri Uri, IReadOnlyDictionary<string, string> Headers);

internal sealed class StubPriceSource(Func<Uri, (HttpStatusCode Status, string Body)> respond) : HttpMessageHandler
{
    private readonly ConcurrentQueue<RecordedRequest> _requests = new();

    public IReadOnlyCollection<RecordedRequest> Requests => _requests;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        _requests.Enqueue(new RecordedRequest(
            uri,
            request.Headers.ToDictionary(header => header.Key, header => string.Join(" ", header.Value), StringComparer.OrdinalIgnoreCase)));

        var (status, body) = respond(uri);

        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}

internal sealed class PriceSourceWebApplicationFactory(
    string clientName,
    StubPriceSource source,
    IReadOnlyDictionary<string, string?>? settings = null) : WebApplicationFactory<Program>
{
    public StubPriceSource Source => source;

    public IMarketPriceProvider Provider(PriceMarket market) =>
        Services.GetServices<IMarketPriceProvider>().Single(provider => provider.Market == market);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
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
            });

            if (settings is not null)
            {
                configuration.AddInMemoryCollection(settings);
            }
        });

        builder.ConfigureTestServices(services =>
            services.AddHttpClient(clientName).ConfigurePrimaryHttpMessageHandler(() => source));
    }
}
