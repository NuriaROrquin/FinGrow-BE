namespace FinGrow.Api.UnitTests.Integrations;

using System.Net;
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

public class MercadoPagoOAuthCallbackTests
{
    private const string CallbackPath = "/api/integrations/mercadopago/oauth/callback";
    private const string ReturnUrl = "https://app.test/dashboard/settings";
    private const string State = "ABCD2345";

    [Fact]
    public async Task A_valid_callback_links_the_account_and_sends_the_browser_back_to_the_frontend()
    {
        using var factory = new OAuthWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        factory.LinkCodes.Add(IntegrationLinkCode.Create(factory.Employee.Id, IntegrationProvider.MercadoPago, State, DateTimeOffset.UtcNow));

        var response = await client.GetAsync(new Uri($"{CallbackPath}?code=TG-123&state={State}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldBe(new Uri($"{ReturnUrl}?mercadopago=linked"));
        var integration = factory.Integrations.ShouldHaveSingleItem();
        integration.ExternalAccountId.ShouldBe("228085066");
        integration.Grant.ShouldNotBeNull();
        factory.OAuth.ExchangedCodes.ShouldHaveSingleItem().ShouldBe("TG-123");
    }

    [Fact]
    public async Task A_callback_with_an_unknown_state_reports_the_error_to_the_frontend_without_calling_mercado_pago()
    {
        using var factory = new OAuthWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(new Uri($"{CallbackPath}?code=TG-123&state=ZZZZ9999", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldBe(new Uri($"{ReturnUrl}?mercadopago=error&reason=Integrations.MercadoPago.InvalidState"));
        factory.Integrations.ShouldBeEmpty();
        factory.OAuth.ExchangedCodes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Starting_a_link_requires_a_session()
    {
        using var factory = new OAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync(new Uri("/api/integrations/mercadopago/oauth/start", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private sealed class OAuthWebApplicationFactory : WebApplicationFactory<Program>
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

        public RecordingMercadoPagoOAuthClient OAuth { get; } = new();

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
                    ["MercadoPago:FrontendReturnUrl"] = ReturnUrl,
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IEmployeeRepository>(_ => new InMemoryEmployeeRepository(Employee));
                services.AddScoped<IEmployeeIntegrationRepository>(_ => new InMemoryEmployeeIntegrationRepository(Integrations));
                services.AddScoped<IIntegrationLinkCodeRepository>(_ => new InMemoryIntegrationLinkCodeRepository(LinkCodes));
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
                services.AddSingleton<IMercadoPagoOAuthClient>(OAuth);
            });
        }
    }

    private sealed class RecordingMercadoPagoOAuthClient : IMercadoPagoOAuthClient
    {
        public List<string> ExchangedCodes { get; } = new();

        public Uri BuildAuthorizationUrl(string state) => new($"https://auth.mercadopago.test/authorization?state={state}");

        public Task<MercadoPagoTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            ExchangedCodes.Add(code);

            return Task.FromResult(new MercadoPagoTokens("access-token", "refresh-token", TimeSpan.FromDays(180), "228085066"));
        }

        public Task<MercadoPagoTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
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
