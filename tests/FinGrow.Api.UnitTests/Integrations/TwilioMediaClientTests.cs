namespace FinGrow.Api.UnitTests.Integrations;

using System.Text;
using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class TwilioMediaClientTests
{
    [Fact]
    public void The_media_http_client_authenticates_with_the_account_credentials()
    {
        using var factory = new TwilioWebApplicationFactory();
        var httpClientFactory = factory.Services.GetRequiredService<IHttpClientFactory>();

        var client = httpClientFactory.CreateClient(nameof(ITwilioMediaClient));

        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("ACunit-test:unit-test-twilio-auth-token"));
        client.DefaultRequestHeaders.Authorization.ShouldNotBeNull();
        client.DefaultRequestHeaders.Authorization.Scheme.ShouldBe("Basic");
        client.DefaultRequestHeaders.Authorization.Parameter.ShouldBe(expected);
    }

    [Fact]
    public void Api_does_not_start_without_the_twilio_auth_token()
    {
        using var factory = new TwilioWebApplicationFactory(authToken: string.Empty);

        var exception = Should.Throw<OptionsValidationException>(() => factory.Services);

        exception.Message.ShouldContain("Twilio:AuthToken");
    }

    private sealed class TwilioWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _authToken;

        public TwilioWebApplicationFactory(string authToken = "unit-test-twilio-auth-token") => _authToken = authToken;

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
                    ["AiService:ApiKey"] = "unit-test-ai-api-key",
                    ["Twilio:AccountSid"] = "ACunit-test",
                    ["Twilio:AuthToken"] = _authToken,
                }));
    }
}
