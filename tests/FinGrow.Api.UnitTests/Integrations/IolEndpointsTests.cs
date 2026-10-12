namespace FinGrow.Api.UnitTests.Integrations;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinGrow.Application.Common;
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

public class IolEndpointsTests
{
    private const string LinkPath = "/api/integrations/iol/link";
    private const string IntegrationPath = "/api/integrations/iol";
    private const string SyncPath = "/api/integrations/iol/sync";
    private const string Password = "s3creta-IOL";

    [Fact]
    public async Task Linking_stores_the_grant_and_answers_without_the_password()
    {
        using var factory = new IolWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(LinkPath, new { username = "juan.perez", password = Password });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain(Password);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("linked").GetBoolean().ShouldBeTrue();
        json.RootElement.GetProperty("status").GetString().ShouldBe("Active");
        var integration = factory.Integrations.ShouldHaveSingleItem();
        integration.EmployeeId.ShouldBe(factory.EmployeeId);
        integration.Grant!.AccessToken.ShouldBe("iol-access");
    }

    [Fact]
    public async Task Rejected_credentials_answer_bad_request_and_link_nothing()
    {
        using var factory = new IolWebApplicationFactory();
        factory.Iol.Failure = new HttpRequestException("invalid_grant", null, HttpStatusCode.BadRequest);
        var client = CreateAuthenticatedClient(factory);

        var response = await client.PostAsJsonAsync(LinkPath, new { username = "juan.perez", password = Password });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Integrations.Iol.InvalidCredentials");
        factory.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Linking_requires_a_session()
    {
        using var factory = new IolWebApplicationFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(LinkPath, new { username = "juan.perez", password = Password });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Iol.Logins.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_integration_shows_why_it_needs_to_be_linked_again()
    {
        using var factory = new IolWebApplicationFactory();
        var integration = EmployeeIntegration.Create(factory.EmployeeId, IntegrationProvider.Iol, "juan.perez", DateTimeOffset.UtcNow);
        integration.Authorize(OAuthGrant.From("access", "refresh", DateTimeOffset.UtcNow), DateTimeOffset.UtcNow);
        integration.MarkFailed("La autorizacion de IOL vencio o fue revocada: volve a vincular la cuenta.", DateTimeOffset.UtcNow);
        factory.Integrations.Add(integration);

        var response = await CreateAuthenticatedClient(factory).GetAsync(new Uri(IntegrationPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetString().ShouldBe("Error");
        json.RootElement.GetProperty("errorReason").GetString().ShouldBe("La autorizacion de IOL vencio o fue revocada: volve a vincular la cuenta.");
    }

    [Fact]
    public async Task Unlinking_removes_the_integration_with_its_tokens()
    {
        using var factory = new IolWebApplicationFactory();
        var integration = EmployeeIntegration.Create(factory.EmployeeId, IntegrationProvider.Iol, "juan.perez", DateTimeOffset.UtcNow);
        integration.Authorize(OAuthGrant.From("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(15)), DateTimeOffset.UtcNow);
        factory.Integrations.Add(integration);

        var response = await CreateAuthenticatedClient(factory).DeleteAsync(new Uri(IntegrationPath, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        factory.Integrations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Syncing_imports_the_portfolio_and_answers_the_summary()
    {
        using var factory = new IolWebApplicationFactory();
        factory.Integrations.Add(LinkedIntegration(factory.EmployeeId));
        factory.Portfolio.Positions.Add(new IolPosition("GGAL", "Grupo Financiero Galicia", "ACCIONES", "bCBA", "peso_Argentino", 10m, 5000m, 56_000m));
        factory.Portfolio.Positions.Add(new IolPosition("GFGC5000OC", "Opcion", "Opciones", "bCBA", "peso_Argentino", 1m, 100m, 120m));

        var response = await CreateAuthenticatedClient(factory).PostAsync(new Uri(SyncPath, UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("imported").GetInt32().ShouldBe(1);
        json.RootElement.GetProperty("ignored").GetInt32().ShouldBe(1);
        var investment = factory.Investments.ShouldHaveSingleItem();
        investment.EmployeeId.ShouldBe(factory.EmployeeId);
        investment.Source.ShouldBe(InvestmentSource.Iol);
        investment.ExternalReference.ShouldBe("bcba:GGAL");
    }

    [Fact]
    public async Task Syncing_without_iol_linked_is_not_found()
    {
        using var factory = new IolWebApplicationFactory();

        var response = await CreateAuthenticatedClient(factory).PostAsync(new Uri(SyncPath, UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Integrations.Iol.NotLinked");
    }

    [Fact]
    public async Task Syncing_while_iol_is_down_answers_service_unavailable_and_imports_nothing()
    {
        using var factory = new IolWebApplicationFactory();
        factory.Integrations.Add(LinkedIntegration(factory.EmployeeId));
        factory.Portfolio.Positions.Add(new IolPosition("GGAL", "Grupo Financiero Galicia", "ACCIONES", "bCBA", "peso_Argentino", 10m, 5000m, 56_000m));
        factory.Portfolio.Failure = new HttpRequestException("bad gateway", null, HttpStatusCode.BadGateway);

        var response = await CreateAuthenticatedClient(factory).PostAsync(new Uri(SyncPath, UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Integrations.Iol.Unavailable");
        factory.Investments.ShouldBeEmpty();
    }

    [Fact]
    public async Task Syncing_with_an_expired_authorization_answers_conflict_to_link_again()
    {
        using var factory = new IolWebApplicationFactory();
        var integration = LinkedIntegration(factory.EmployeeId);
        integration.MarkFailed("La autorizacion de IOL vencio o fue revocada: volve a vincular la cuenta.", DateTimeOffset.UtcNow);
        factory.Integrations.Add(integration);

        var response = await CreateAuthenticatedClient(factory).PostAsync(new Uri(SyncPath, UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Integrations.Iol.GrantExpired");
        factory.Portfolio.Requests.ShouldBe(0);
    }

    [Fact]
    public async Task Syncing_requires_a_session()
    {
        using var factory = new IolWebApplicationFactory();

        var response = await factory.CreateClient().PostAsync(new Uri(SyncPath, UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Portfolio.Requests.ShouldBe(0);
    }

    private static EmployeeIntegration LinkedIntegration(Guid employeeId)
    {
        var integration = EmployeeIntegration.Create(employeeId, IntegrationProvider.Iol, "juan.perez", DateTimeOffset.UtcNow);
        integration.Authorize(OAuthGrant.From("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(15)), DateTimeOffset.UtcNow);

        return integration;
    }

    private static HttpClient CreateAuthenticatedClient(IolWebApplicationFactory factory)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(factory.EmployeeId, Guid.CreateVersion7(), Rol.Empleado, "Juan Perez");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class IolWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Guid EmployeeId { get; } = Guid.CreateVersion7();

        public List<EmployeeIntegration> Integrations { get; } = new();

        public RecordingIolAuthClient Iol { get; } = new();

        public List<Investment> Investments { get; } = new();

        public StubIolPortfolioClient Portfolio { get; } = new();

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
                    ["Email:ResendApiKey"] = "re_unit_test_key",
                    ["MercadoPago:ClientId"] = "unit-test-mp-client-id",
                    ["MercadoPago:ClientSecret"] = "unit-test-mp-client-secret",
                    ["Gmail:ClientId"] = "unit-test-gmail-client-id",
                    ["Gmail:ClientSecret"] = "unit-test-gmail-client-secret",
                    ["Gmail:RedirectUri"] = "https://api.test/api/integrations/gmail/oauth/callback",
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key-1234",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IEmployeeIntegrationRepository>(_ => new InMemoryEmployeeIntegrationRepository(Integrations));
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
                services.AddSingleton<IIolAuthClient>(Iol);
                services.AddSingleton<IIolPortfolioClient>(Portfolio);
                services.AddScoped<IInvestmentRepository>(_ => new InMemoryInvestmentRepository(Investments));
            });
        }
    }

    private sealed class RecordingIolAuthClient : IIolAuthClient
    {
        public Exception? Failure { get; set; }

        public List<string> Logins { get; } = new();

        public Task<IolTokens> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            Logins.Add(username);

            return Failure is null
                ? Task.FromResult(new IolTokens("iol-access", "iol-refresh", TimeSpan.FromMinutes(15)))
                : Task.FromException<IolTokens>(Failure);
        }

        public Task<IolTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
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
            throw new NotSupportedException();

        public void Add(EmployeeIntegration integration) => _integrations.Add(integration);

        public void Remove(EmployeeIntegration integration) => _integrations.Remove(integration);
    }

    private sealed class StubIolPortfolioClient : IIolPortfolioClient
    {
        public List<IolPosition> Positions { get; } = new();

        public Exception? Failure { get; set; }

        public int Requests { get; private set; }

        public Task<IReadOnlyList<IolPosition>> GetPortfolioAsync(string accessToken, IolCountry country, CancellationToken cancellationToken = default)
        {
            Requests++;

            if (Failure is not null)
            {
                return Task.FromException<IReadOnlyList<IolPosition>>(Failure);
            }

            return Task.FromResult<IReadOnlyList<IolPosition>>(
                country == IolCountry.Argentina ? Positions.ToList() : new List<IolPosition>());
        }

        public Task<IReadOnlyList<IolOperation>> ListCompletedOperationsAsync(string accessToken, DateOnly from, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<IolOperation>>(new List<IolOperation>());
    }

    private sealed class InMemoryInvestmentRepository : IInvestmentRepository
    {
        private readonly List<Investment> _investments;

        public InMemoryInvestmentRepository(List<Investment> investments) => _investments = investments;

        public void Add(Investment investment) => _investments.Add(investment);

        public void Remove(Investment investment) => _investments.Remove(investment);

        public Task<Investment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_investments.FirstOrDefault(investment => investment.Id == id));

        public Task<IReadOnlyList<Investment>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Guid>> ListEmployeesWithTrackedInvestmentsAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Investment>> ListTrackedByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Investment>> ListImportedAsync(Guid employeeId, InvestmentSource source, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Investment>>(_investments
                .Where(investment => investment.EmployeeId == employeeId && investment.Source == source)
                .ToList());
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
