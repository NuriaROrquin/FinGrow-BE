namespace FinGrow.Api.UnitTests.Integrations;

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using FinGrow.Api.Twilio;
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

public class WhatsAppWebhookTests
{
    private const string AuthToken = "unit-test-twilio-auth-token";
    private const string WebhookPath = "/api/webhooks/whatsapp";
    private const string Phone = "+5491112345678";
    private const string Code = "ABCD2345";

    [Fact]
    public void The_signature_validator_reproduces_the_example_from_the_twilio_docs()
    {
        var form = new Dictionary<string, string>
        {
            ["CallSid"] = "CA1234567890ABCDE",
            ["Caller"] = "+12349013030",
            ["Digits"] = "1234",
            ["From"] = "+12349013030",
            ["To"] = "+18005551212",
        };
        using var factory = new WebhookWebApplicationFactory("12345");
        var validator = factory.Services.GetRequiredService<ITwilioRequestValidator>();

        validator.IsValid("https://mycompany.com/myapp.php?foo=1&bar=2", form, "0/KCTR6DLpKmkAf8muzZqo1nDgQ=").ShouldBeTrue();
        validator.IsValid("https://mycompany.com/myapp.php?foo=1&bar=2", form, "0/KCTR6DLpKmkAf8muzZqo1nDgX=").ShouldBeFalse();
        validator.IsValid("https://mycompany.com/myapp.php?foo=1&bar=2", form, null).ShouldBeFalse();
    }

    [Fact]
    public async Task A_request_without_a_valid_signature_is_rejected_and_links_nothing()
    {
        using var factory = new WebhookWebApplicationFactory();
        var client = factory.CreateClient();
        factory.LinkCodes.Add(IntegrationLinkCode.Create(factory.Employee.Id, IntegrationProvider.WhatsApp, Code, DateTimeOffset.UtcNow));
        var form = InboundMessage(Code);

        using var content = new FormUrlEncodedContent(form);
        content.Headers.Add(ValidateTwilioSignatureAttribute.SignatureHeader, "not-a-signature");
        var response = await client.PostAsync(new Uri(WebhookPath, UriKind.Relative), content);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        factory.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_signed_message_with_a_valid_code_links_the_number_and_answers_with_twiml()
    {
        using var factory = new WebhookWebApplicationFactory();
        var client = factory.CreateClient();
        factory.LinkCodes.Add(IntegrationLinkCode.Create(factory.Employee.Id, IntegrationProvider.WhatsApp, Code, DateTimeOffset.UtcNow));
        var form = InboundMessage(Code);

        var response = await PostSignedAsync(client, form);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/xml");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldStartWith("<?xml");
        body.ShouldContain("<Response><Message>Listo, Juan Perez");
        factory.Integrations.ShouldHaveSingleItem().ExternalAccountId.ShouldBe(Phone);
    }

    [Fact]
    public async Task A_signed_message_from_an_unlinked_number_gets_the_linking_instructions()
    {
        using var factory = new WebhookWebApplicationFactory();
        var client = factory.CreateClient();
        var form = InboundMessage("hola");
        form["From"] = "whatsapp:+5491100000000";

        var response = await PostSignedAsync(client, form);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("todavía no está vinculado");
    }

    private static Dictionary<string, string> InboundMessage(string body) => new(StringComparer.Ordinal)
    {
        ["MessageSid"] = "SM0123456789abcdef",
        ["AccountSid"] = "ACunit-test",
        ["From"] = "whatsapp:" + Phone,
        ["To"] = "whatsapp:+14155238886",
        ["Body"] = body,
        ["NumMedia"] = "0",
    };

    private static async Task<HttpResponseMessage> PostSignedAsync(HttpClient client, Dictionary<string, string> form)
    {
        var url = new Uri(client.BaseAddress!, WebhookPath).ToString();
        using var content = new FormUrlEncodedContent(form);
        content.Headers.Add(ValidateTwilioSignatureAttribute.SignatureHeader, Sign(url, form));

        return await client.PostAsync(new Uri(WebhookPath, UriKind.Relative), content);
    }

    [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "Es el algoritmo de Twilio.")]
    private static string Sign(string url, Dictionary<string, string> form)
    {
        var payload = url + string.Concat(form.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + pair.Value));
        var hash = HMACSHA1.HashData(Encoding.UTF8.GetBytes(AuthToken), Encoding.UTF8.GetBytes(payload));

        return Convert.ToBase64String(hash);
    }

    private sealed class WebhookWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _authToken;

        public WebhookWebApplicationFactory(string authToken = AuthToken) => _authToken = authToken;

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

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:MigrateOnStartup"] = "false",
                    ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
                    ["AiService:ApiKey"] = "unit-test-ai-api-key",
                    ["Twilio:AccountSid"] = "ACunit-test",
                    ["Twilio:AuthToken"] = _authToken,
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IEmployeeRepository>(_ => new InMemoryEmployeeRepository(Employee));
                services.AddScoped<IEmployeeIntegrationRepository>(_ => new InMemoryEmployeeIntegrationRepository(Integrations));
                services.AddScoped<IIntegrationLinkCodeRepository>(_ => new InMemoryIntegrationLinkCodeRepository(LinkCodes));
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
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

        public void Add(EmployeeIntegration integration) => _integrations.Add(integration);
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
