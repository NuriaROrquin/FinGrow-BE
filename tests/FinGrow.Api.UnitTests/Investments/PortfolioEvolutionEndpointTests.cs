namespace FinGrow.Api.UnitTests.Investments;

using System.Globalization;
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
using Microsoft.Extensions.DependencyInjection.Extensions;

public class PortfolioEvolutionEndpointTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    [Fact]
    public async Task The_evolution_comes_in_the_requested_currency_from_the_first_purchase_within_the_range()
    {
        using var factory = new EvolutionWebApplicationFactory();
        var today = Today(factory);
        factory.Investments.Investments.Add(Investment.Create(
            EmployeeId, "SPY", InvestmentType.Etf, Money.From(500m, Currency.USD), today.AddDays(-10), DateTimeOffset.UtcNow));
        factory.Investments.Investments.Add(Investment.Create(
            EmployeeId, "AL30", InvestmentType.Bond, Money.From(1000m, Currency.ARS), today.AddDays(-20), DateTimeOffset.UtcNow));

        var response = await AuthenticatedClient(factory).GetAsync(
            new Uri("/api/investments/evolution?currency=USD&range=OneMonth", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        factory.Investments.RequestedEmployeeId.ShouldBe(EmployeeId);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("currency").GetString().ShouldBe("USD");
        body.GetProperty("interval").GetString().ShouldBe("Day");
        body.GetProperty("historyStartsOn").GetString().ShouldBe(IsoDate(today.AddDays(-10)));
        var points = body.GetProperty("points");
        points.GetArrayLength().ShouldBe(11);
        points[0].GetProperty("date").GetString().ShouldBe(IsoDate(today.AddDays(-10)));
        points[0].GetProperty("value").GetDecimal().ShouldBe(500m);
        points[0].GetProperty("investedAmount").GetDecimal().ShouldBe(500m);
    }

    [Fact]
    public async Task Without_parameters_it_shows_the_last_three_months_in_pesos()
    {
        using var factory = new EvolutionWebApplicationFactory();
        var today = Today(factory);
        factory.Investments.Investments.Add(Investment.Create(
            EmployeeId, "AL30", InvestmentType.Bond, Money.From(1000m, Currency.ARS), today.AddYears(-1), DateTimeOffset.UtcNow));

        var response = await AuthenticatedClient(factory).GetAsync(new Uri("/api/investments/evolution", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("currency").GetString().ShouldBe("ARS");
        var points = body.GetProperty("points");
        points[0].GetProperty("date").GetString().ShouldBe(IsoDate(today.AddMonths(-3)));
        points[points.GetArrayLength() - 1].GetProperty("date").GetString().ShouldBe(IsoDate(today));
    }

    [Theory]
    [InlineData("range=TenYears")]
    [InlineData("range=99")]
    [InlineData("currency=GBP")]
    public async Task An_unknown_range_or_currency_is_rejected_with_400_without_querying_the_database(string query)
    {
        using var factory = new EvolutionWebApplicationFactory();

        var response = await AuthenticatedClient(factory).GetAsync(
            new Uri($"/api/investments/evolution?{query}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Investments.RequestedEmployeeId.ShouldBeNull();
    }

    [Fact]
    public async Task Without_a_session_the_evolution_is_not_served()
    {
        using var factory = new EvolutionWebApplicationFactory();

        var response = await factory.CreateClient().GetAsync(new Uri("/api/investments/evolution", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Investments.RequestedEmployeeId.ShouldBeNull();
    }

    private static DateOnly Today(EvolutionWebApplicationFactory factory) =>
        factory.Services.GetRequiredService<IDateTimeProvider>().Today;

    private static string IsoDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static HttpClient AuthenticatedClient(EvolutionWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(EmployeeId, Guid.CreateVersion7(), Rol.Empleado, "Ana Gomez");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class CapturingInvestmentRepository : IInvestmentRepository
    {
        public List<Investment> Investments { get; } = new();

        public Guid? RequestedEmployeeId { get; private set; }

        public void Add(Investment investment) => Investments.Add(investment);

        public void Remove(Investment investment) => Investments.Remove(investment);

        public Task<Investment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Investments.FirstOrDefault(investment => investment.Id == id));

        public Task<IReadOnlyList<Investment>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            RequestedEmployeeId = employeeId;

            return Task.FromResult<IReadOnlyList<Investment>>(
                Investments.Where(investment => investment.EmployeeId == employeeId).ToList());
        }

        public Task<IReadOnlyList<Guid>> ListEmployeesWithTrackedInvestmentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());

        public Task<IReadOnlyList<Investment>> ListTrackedByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Investment>>(Array.Empty<Investment>());

        public Task<IReadOnlyList<Guid>> ListEmployeesWithAccruingInvestmentsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());

        public Task<IReadOnlyList<Investment>> ListAccruingByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Investment>>(Array.Empty<Investment>());

        public Task<IReadOnlyList<Investment>> ListImportedAsync(
            Guid employeeId,
            InvestmentSource source,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Investment>>(Array.Empty<Investment>());
    }

    private sealed class EvolutionWebApplicationFactory : WebApplicationFactory<Program>
    {
        public CapturingInvestmentRepository Investments { get; } = new();

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
                    ["MercadoPago:RedirectUri"] = "https://api.test/api/integrations/mercadopago/oauth/callback",
                    ["Gmail:ClientId"] = "unit-test-gmail-client-id",
                    ["Gmail:ClientSecret"] = "unit-test-gmail-client-secret",
                    ["Gmail:RedirectUri"] = "https://api.test/api/integrations/gmail/oauth/callback",
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key-1234",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IInvestmentRepository>();
                services.AddSingleton<IInvestmentRepository>(Investments);
            });
        }
    }
}
