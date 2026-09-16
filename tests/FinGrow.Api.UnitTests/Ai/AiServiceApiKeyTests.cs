namespace FinGrow.Api.UnitTests.Ai;

using FinGrow.Application.Interfaces;
using FinGrow.Infrastructure.Ai;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class AiServiceApiKeyTests
{
    private const string ApiKey = "unit-test-ai-api-key";

    [Fact]
    public void Ai_http_client_sends_the_configured_api_key_header()
    {
        using var factory = new AiServiceWebApplicationFactory(ApiKey);
        var httpClientFactory = factory.Services.GetRequiredService<IHttpClientFactory>();

        // AddHttpClient<TClient, TImplementation> registra el cliente con el nombre de TClient.
        var client = httpClientFactory.CreateClient(nameof(IAiService));

        client.DefaultRequestHeaders.GetValues(AiServiceOptions.ApiKeyHeader).ShouldBe(new[] { ApiKey });
    }

    [Fact]
    public void Api_does_not_start_without_an_ai_api_key()
    {
        using var factory = new AiServiceWebApplicationFactory(apiKey: string.Empty);

        var exception = Should.Throw<OptionsValidationException>(() => factory.Services);

        exception.Message.ShouldContain("AiService:ApiKey");
    }

    private sealed class AiServiceWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _apiKey;

        public AiServiceWebApplicationFactory(string apiKey) => _apiKey = apiKey;

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
                    ["AiService:ApiKey"] = _apiKey,
                    ["Twilio:AccountSid"] = "ACunit-test",
                    ["Twilio:AuthToken"] = "unit-test-twilio-auth-token",
                    ["Telegram:BotToken"] = "unit-test-telegram-bot-token",
                    ["Telegram:WebhookSecret"] = "unit-test-telegram-secret",
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                }));
    }
}
