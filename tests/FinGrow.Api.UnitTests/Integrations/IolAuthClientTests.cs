namespace FinGrow.Api.UnitTests.Integrations;

using System.Net;
using System.Text;
using FinGrow.Application.Interfaces;
using FinGrow.Infrastructure.Integrations.Iol;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class IolAuthClientTests
{
    private const string Password = "s3creta-IOL";
    private const string TokenResponse = """
        {"access_token": "iol-access", "token_type": "bearer", "expires_in": 899, "refresh_token": "iol-refresh"}
        """;

    [Fact]
    public async Task Login_posts_the_password_grant_as_a_form()
    {
        using var handler = new RecordingHandler(TokenResponse);

        var tokens = await Client(handler).LoginAsync("juan.perez", Password);

        tokens.ShouldBe(new IolTokens("iol-access", "iol-refresh", TimeSpan.FromSeconds(899)));
        handler.Path.ShouldBe("/token");
        handler.ContentType.ShouldBe("application/x-www-form-urlencoded");
        var form = QueryHelpers.ParseQuery(handler.Body);
        form["username"].ToString().ShouldBe("juan.perez");
        form["password"].ToString().ShouldBe(Password);
        form["grant_type"].ToString().ShouldBe("password");
    }

    [Fact]
    public async Task Refresh_posts_the_refresh_token_grant()
    {
        using var handler = new RecordingHandler(TokenResponse);

        await Client(handler).RefreshAsync("old-refresh");

        var form = QueryHelpers.ParseQuery(handler.Body);
        form["refresh_token"].ToString().ShouldBe("old-refresh");
        form["grant_type"].ToString().ShouldBe("refresh_token");
        form.ContainsKey("password").ShouldBeFalse();
    }

    [Fact]
    public async Task A_rejection_keeps_the_status_code_and_never_carries_the_password()
    {
        using var handler = new RecordingHandler("""{"error": "invalid_grant"}""", HttpStatusCode.BadRequest);

        var exception = await Should.ThrowAsync<HttpRequestException>(() => Client(handler).LoginAsync("juan.perez", Password));

        exception.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        exception.Message.ShouldContain("400");
        exception.Message.ShouldNotContain(Password);
    }

    [Fact]
    public async Task A_response_without_refresh_token_is_rejected_as_incomplete()
    {
        using var handler = new RecordingHandler("""{"access_token": "iol-access", "expires_in": 899}""");

        var exception = await Should.ThrowAsync<HttpRequestException>(() => Client(handler).LoginAsync("juan.perez", Password));

        exception.StatusCode.ShouldBeNull();
    }

    [Theory]
    [InlineData(null, "https://api.invertironline.com/")]
    [InlineData("https://api.iol.test/", "https://api.iol.test/")]
    public void The_base_url_is_the_real_api_unless_configured(string? apiBaseUrl, string expected)
    {
        using var factory = new IolWebApplicationFactory(apiBaseUrl);
        var httpClientFactory = factory.Services.GetRequiredService<IHttpClientFactory>();

        var client = httpClientFactory.CreateClient(nameof(IIolAuthClient));

        client.BaseAddress.ShouldBe(new Uri(expected));
    }

    private static IolAuthClient Client(RecordingHandler handler) =>
        new(new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.iol.test/") });

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _status;

        public RecordingHandler(string response, HttpStatusCode status = HttpStatusCode.OK)
        {
            _response = response;
            _status = status;
        }

        public string? Path { get; private set; }

        public string? ContentType { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.AbsolutePath;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class IolWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string? _apiBaseUrl;

        public IolWebApplicationFactory(string? apiBaseUrl) => _apiBaseUrl = apiBaseUrl;

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
                    ["AiService:ApiKey"] = "unit-test-ai-api-key",
                    ["Twilio:AccountSid"] = "ACunit-test",
                    ["Twilio:AuthToken"] = "unit-test-twilio-auth-token",
                    ["Telegram:BotToken"] = "unit-test-telegram-bot-token",
                    ["Telegram:WebhookSecret"] = "unit-test-telegram-secret",
                    ["Email:ResendApiKey"] = "re_unit_test_key",
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key-1234",
                };

                if (_apiBaseUrl is not null)
                {
                    settings["Iol:ApiBaseUrl"] = _apiBaseUrl;
                }

                configuration.AddInMemoryCollection(settings);
            });
    }
}
