namespace FinGrow.Api.UnitTests.Mailing;

using System.Net;
using System.Text;
using System.Text.Json;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.ValueObjects;
using FinGrow.Infrastructure.Mailing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

public class ResendEmailSenderTests
{
    private static readonly EmailMessage Welcome = new(
        Email.From("ana.gomez@empresa.com"),
        "Bienvenida a FinGrow",
        "<p>Hola Ana</p>",
        "Hola Ana");

    [Fact]
    public async Task The_message_is_posted_to_resend_with_the_configured_sender()
    {
        using var handler = new RecordingHandler("""{"id": "re-123"}""");

        var id = await Send(handler, Welcome);

        id.ShouldBe("re-123");
        handler.Path.ShouldBe("/emails");
        using var body = JsonDocument.Parse(handler.Body!);
        body.RootElement.GetProperty("from").GetString().ShouldBe("FinGrow <no-reply@fingrow.test>");
        body.RootElement.GetProperty("to")[0].GetString().ShouldBe("ana.gomez@empresa.com");
        body.RootElement.GetProperty("subject").GetString().ShouldBe("Bienvenida a FinGrow");
        body.RootElement.GetProperty("html").GetString().ShouldBe("<p>Hola Ana</p>");
        body.RootElement.GetProperty("text").GetString().ShouldBe("Hola Ana");
    }

    [Fact]
    public async Task A_message_without_attachment_does_not_send_the_attachments_field()
    {
        using var handler = new RecordingHandler("""{"id": "re-123"}""");

        await Send(handler, Welcome);

        using var body = JsonDocument.Parse(handler.Body!);
        body.RootElement.TryGetProperty("attachments", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task An_attachment_is_posted_with_its_name_and_its_base64_content()
    {
        using var handler = new RecordingHandler("""{"id": "re-123"}""");
        var report = Welcome with { Attachment = EmailAttachment.From("reporte-empresa_202609.pdf", new byte[] { 37, 80, 68, 70 }) };

        await Send(handler, report);

        using var body = JsonDocument.Parse(handler.Body!);
        var attachment = body.RootElement.GetProperty("attachments").EnumerateArray().ShouldHaveSingleItem();
        attachment.GetProperty("filename").GetString().ShouldBe("reporte-empresa_202609.pdf");
        attachment.GetProperty("content").GetString().ShouldBe("JVBERg==");
    }

    [Fact]
    public async Task With_a_redirect_the_message_goes_to_the_redirect_and_names_the_original_recipient()
    {
        using var handler = new RecordingHandler("""{"id": "re-123"}""");

        await Send(handler, Welcome, redirectAllTo: "pruebas@fingrow.test");

        using var body = JsonDocument.Parse(handler.Body!);
        body.RootElement.GetProperty("to").GetArrayLength().ShouldBe(1);
        body.RootElement.GetProperty("to")[0].GetString().ShouldBe("pruebas@fingrow.test");
        body.RootElement.GetProperty("subject").GetString().ShouldBe("[Para ana.gomez@empresa.com] Bienvenida a FinGrow");
    }

    [Fact]
    public async Task A_rejection_from_resend_is_reported_with_its_reason()
    {
        using var handler = new RecordingHandler(
            """{"statusCode": 422, "name": "validation_error", "message": "Invalid `from` field."}""",
            HttpStatusCode.UnprocessableEntity);

        var exception = await Should.ThrowAsync<EmailDeliveryException>(() => Send(handler, Welcome));

        exception.Message.ShouldContain("422");
        exception.Message.ShouldContain("Invalid `from` field.");
    }

    [Fact]
    public async Task An_unreachable_resend_is_reported_as_a_delivery_failure()
    {
        using var handler = new RecordingHandler(new HttpRequestException("connection refused"));

        await Should.ThrowAsync<EmailDeliveryException>(() => Send(handler, Welcome));
    }

    [Fact]
    public void The_http_client_authenticates_with_the_resend_api_key()
    {
        using var factory = new EmailWebApplicationFactory();
        var httpClientFactory = factory.Services.GetRequiredService<IHttpClientFactory>();

        var client = httpClientFactory.CreateClient(nameof(IEmailSender));

        client.BaseAddress.ShouldBe(new Uri("https://api.resend.com/"));
        client.DefaultRequestHeaders.Authorization.ShouldNotBeNull();
        client.DefaultRequestHeaders.Authorization.Scheme.ShouldBe("Bearer");
        client.DefaultRequestHeaders.Authorization.Parameter.ShouldBe("re_unit_test_key");
    }

    [Fact]
    public void Without_a_configured_redirect_each_message_goes_to_its_own_recipient()
    {
        using var factory = new EmailWebApplicationFactory();

        var options = factory.Services.GetRequiredService<IOptions<EmailOptions>>().Value;

        options.RedirectAllTo.ShouldBeNullOrWhiteSpace();
    }

    [Fact]
    public void Api_does_not_start_without_the_resend_api_key()
    {
        using var factory = new EmailWebApplicationFactory(apiKey: string.Empty);

        var exception = Should.Throw<OptionsValidationException>(() => factory.Services);

        exception.Message.ShouldContain("Email:ResendApiKey");
    }

    [Fact]
    public void Api_does_not_start_with_a_malformed_redirect()
    {
        using var factory = new EmailWebApplicationFactory(redirectAllTo: "no-es-un-mail");

        var exception = Should.Throw<OptionsValidationException>(() => factory.Services);

        exception.Message.ShouldContain("Email:RedirectAllTo");
    }

    private static async Task<string> Send(RecordingHandler handler, EmailMessage message, string? redirectAllTo = null)
    {
        using var httpClient = new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("https://api.resend.test/") };
        var options = Options.Create(new EmailOptions
        {
            ResendApiKey = "re_unit_test_key",
            From = "FinGrow <no-reply@fingrow.test>",
            RedirectAllTo = redirectAllTo,
        });

        return await new ResendEmailSender(httpClient, options).SendAsync(message);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _response = string.Empty;
        private readonly HttpStatusCode _status;
        private readonly Exception? _failure;

        public RecordingHandler(string response, HttpStatusCode status = HttpStatusCode.OK)
        {
            _response = response;
            _status = status;
        }

        public RecordingHandler(Exception failure) => _failure = failure;

        public string? Path { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_failure is not null)
            {
                throw _failure;
            }

            Path = request.RequestUri?.AbsolutePath;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_status)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class EmailWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _apiKey;
        private readonly string? _redirectAllTo;

        public EmailWebApplicationFactory(string apiKey = "re_unit_test_key", string? redirectAllTo = null)
        {
            _apiKey = apiKey;
            _redirectAllTo = redirectAllTo;
        }

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
                    ["Email:ResendApiKey"] = _apiKey,
                    ["Email:From"] = "FinGrow <no-reply@fingrow.test>",
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["Gmail:ClientId"] = "unit-test-gmail-client-id",
                    ["Gmail:ClientSecret"] = "unit-test-gmail-client-secret",
                    ["Gmail:RedirectUri"] = "https://api.test/api/integrations/gmail/oauth/callback",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key-1234",
                };

                if (_redirectAllTo is not null)
                {
                    settings["Email:RedirectAllTo"] = _redirectAllTo;
                }

                configuration.AddInMemoryCollection(settings);
            });
    }
}
