namespace FinGrow.Api.UnitTests.Integrations;

using System.Net;
using System.Net.Http.Json;
using FinGrow.Api.Telegram;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class TelegramWebhookTests
{
    private const string Secret = "unit-test-telegram-secret";
    private const string WebhookPath = "/api/webhooks/telegram";
    private const long ChatId = 123456789;
    private const string Code = "ABCD2345";

    [Fact]
    public async Task A_request_without_the_secret_header_is_rejected_and_links_nothing()
    {
        using var factory = new WebhookWebApplicationFactory();
        var client = factory.CreateClient();
        factory.LinkCodes.Add(IntegrationLinkCode.Create(factory.Employee.Id, IntegrationProvider.Telegram, Code, DateTimeOffset.UtcNow));

        var response = await client.PostAsJsonAsync(new Uri(WebhookPath, UriKind.Relative), PrivateMessage(Code));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        factory.Integrations.ShouldBeEmpty();
        factory.Bot.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_request_with_a_wrong_secret_is_rejected()
    {
        using var factory = new WebhookWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await PostAsync(client, PrivateMessage("hola"), "another-secret");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_message_with_a_valid_code_links_the_chat_and_replies_through_the_bot()
    {
        using var factory = new WebhookWebApplicationFactory();
        var client = factory.CreateClient();
        factory.LinkCodes.Add(IntegrationLinkCode.Create(factory.Employee.Id, IntegrationProvider.Telegram, Code, DateTimeOffset.UtcNow));

        var response = await PostAsync(client, PrivateMessage("/start " + Code));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Integrations.ShouldHaveSingleItem().ExternalAccountId.ShouldBe("123456789");
        var sent = factory.Bot.Sent.ShouldHaveSingleItem();
        sent.ChatId.ShouldBe(ChatId);
        sent.Text.ShouldStartWith("Listo, Juan Perez");
    }

    [Fact]
    public async Task A_message_from_an_unlinked_chat_gets_the_linking_instructions()
    {
        using var factory = new WebhookWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await PostAsync(client, PrivateMessage("hola"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Bot.Sent.ShouldHaveSingleItem().Text.ShouldContain("todavía no está vinculado");
    }

    [Fact]
    public async Task Updates_that_are_not_private_text_messages_are_acknowledged_and_ignored()
    {
        using var factory = new WebhookWebApplicationFactory();
        var client = factory.CreateClient();
        factory.LinkCodes.Add(IntegrationLinkCode.Create(factory.Employee.Id, IntegrationProvider.Telegram, Code, DateTimeOffset.UtcNow));

        var groupMessage = await PostAsync(client, new
        {
            update_id = 1,
            message = new { message_id = 7, chat = new { id = -100200300, type = "group" }, text = Code },
        });
        var photo = await PostAsync(client, new
        {
            update_id = 2,
            message = new { message_id = 8, chat = new { id = ChatId, type = "private" } },
        });
        var edited = await PostAsync(client, new { update_id = 3, edited_message = new { message_id = 9 } });

        groupMessage.StatusCode.ShouldBe(HttpStatusCode.OK);
        photo.StatusCode.ShouldBe(HttpStatusCode.OK);
        edited.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Integrations.ShouldBeEmpty();
        factory.Bot.Sent.ShouldBeEmpty();
    }

    private static object PrivateMessage(string text) => new
    {
        update_id = 100,
        message = new
        {
            message_id = 7,
            chat = new { id = ChatId, type = "private" },
            text,
        },
    };

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, object update, string secret = Secret)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(WebhookPath, UriKind.Relative))
        {
            Content = JsonContent.Create(update),
        };
        request.Headers.Add(ValidateTelegramSecretAttribute.SecretHeader, secret);

        return await client.SendAsync(request);
    }

    private sealed class WebhookWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Employee Employee { get; } = Employee.Create(
            Guid.CreateVersion7(),
            departmentId: null,
            "Juan Perez",
            Email.From("juan.perez@empresa.com"),
            null,
            "hash-bcrypt",
            Currency.ARS,
            new DateOnly(2026, 1, 15),
            DateTimeOffset.UtcNow);

        public List<EmployeeIntegration> Integrations { get; } = new();

        public List<IntegrationLinkCode> LinkCodes { get; } = new();

        public RecordingTelegramBotClient Bot { get; } = new();

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
                    ["Telegram:WebhookSecret"] = Secret,
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IEmployeeRepository>(_ => new InMemoryEmployeeRepository(Employee));
                services.AddScoped<IEmployeeIntegrationRepository>(_ => new InMemoryEmployeeIntegrationRepository(Integrations));
                services.AddScoped<IIntegrationLinkCodeRepository>(_ => new InMemoryIntegrationLinkCodeRepository(LinkCodes));
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
                services.AddSingleton<ITelegramBotClient>(Bot);
            });
        }
    }

    private sealed class RecordingTelegramBotClient : ITelegramBotClient
    {
        public List<(long ChatId, string Text)> Sent { get; } = new();

        public Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
        {
            Sent.Add((chatId, text));
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryEmployeeRepository : IEmployeeRepository
    {
        private readonly Employee _employee;

        public InMemoryEmployeeRepository(Employee employee) => _employee = employee;

        public Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_employee.Id == id ? _employee : null);

        public Task<Employee?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
            Task.FromResult(_employee.Email == email ? _employee : null);
    }

    private sealed class InMemoryEmployeeIntegrationRepository : IEmployeeIntegrationRepository
    {
        private readonly List<EmployeeIntegration> _integrations;

        public InMemoryEmployeeIntegrationRepository(List<EmployeeIntegration> integrations) => _integrations = integrations;

        public Task<EmployeeIntegration?> FindByExternalAccountAsync(IntegrationProvider provider, string externalAccountId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_integrations.FirstOrDefault(integration => integration.Provider == provider && integration.ExternalAccountId == externalAccountId));

        public Task<EmployeeIntegration?> FindByEmployeeAsync(Guid employeeId, IntegrationProvider provider, CancellationToken cancellationToken = default) =>
            Task.FromResult(_integrations.FirstOrDefault(integration => integration.EmployeeId == employeeId && integration.Provider == provider));

        public Task<IReadOnlyList<EmployeeIntegration>> ListAuthorizedAsync(IntegrationProvider provider, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmployeeIntegration>>(_integrations.Where(integration => integration.Provider == provider && integration.Grant is not null).ToList());

        public void Add(EmployeeIntegration integration) => _integrations.Add(integration);

        public void Remove(EmployeeIntegration integration) => _integrations.Remove(integration);
    }

    private sealed class InMemoryIntegrationLinkCodeRepository : IIntegrationLinkCodeRepository
    {
        private readonly List<IntegrationLinkCode> _linkCodes;

        public InMemoryIntegrationLinkCodeRepository(List<IntegrationLinkCode> linkCodes) => _linkCodes = linkCodes;

        public Task<IntegrationLinkCode?> FindByHashAsync(IntegrationProvider provider, string codeHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(_linkCodes.FirstOrDefault(linkCode => linkCode.Provider == provider && linkCode.CodeHash == codeHash));

        public void Add(IntegrationLinkCode linkCode) => _linkCodes.Add(linkCode);
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
