namespace FinGrow.Api.UnitTests.Investments;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Features.Investments.ListInvestments;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class InvestmentsListEndpointTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    [Fact]
    public async Task Every_query_parameter_reaches_the_filters()
    {
        using var factory = new InvestmentsWebApplicationFactory();
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri(
            "/api/investments?pageNumber=2&pageSize=5&search=al30&types=Bond&types=Stock&currencies=ARS"
            + "&purchasedFrom=2026-01-01&purchasedTo=2026-09-30&minInvested=100.5&maxInvested=2000000"
            + "&quoted=true&performance=Loss&sortBy=ReturnPercentage&sortDirection=Ascending",
            UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var filters = factory.Investments.RequestedFilters.ShouldNotBeNull();
        factory.Investments.RequestedEmployeeId.ShouldBe(EmployeeId);
        filters.PageNumber.ShouldBe(2);
        filters.PageSize.ShouldBe(5);
        filters.Search.ShouldBe("al30");
        filters.Types.ShouldBe(new[] { InvestmentType.Bond, InvestmentType.Stock });
        filters.Currencies.ShouldBe(new[] { Currency.ARS });
        filters.PurchasedFrom.ShouldBe(new DateOnly(2026, 1, 1));
        filters.PurchasedTo.ShouldBe(new DateOnly(2026, 9, 30));
        filters.MinInvested.ShouldBe(100.5m);
        filters.MaxInvested.ShouldBe(2000000m);
        filters.Quoted.ShouldBe(true);
        filters.Performance.ShouldBe(InvestmentPerformance.Loss);
        filters.SortBy.ShouldBe(InvestmentSortField.ReturnPercentage);
        filters.SortDirection.ShouldBe(SortDirection.Ascending);
    }

    [Fact]
    public async Task Without_parameters_the_first_page_comes_sorted_by_the_latest_purchase()
    {
        using var factory = new InvestmentsWebApplicationFactory();
        factory.Investments.Investments.Add(Investment.Create(
            EmployeeId, "AL30", InvestmentType.Bond, Money.From(1000m, Currency.ARS), new DateOnly(2026, 9, 1), DateTimeOffset.UtcNow));
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri("/api/investments", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var filters = factory.Investments.RequestedFilters.ShouldNotBeNull();
        filters.PageNumber.ShouldBe(1);
        filters.PageSize.ShouldBe(10);
        filters.SortBy.ShouldBe(InvestmentSortField.PurchasedOn);
        filters.SortDirection.ShouldBe(SortDirection.Descending);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().ShouldBe(1);
        body.GetProperty("items")[0].GetProperty("assetName").GetString().ShouldBe("AL30");
        body.GetProperty("items")[0].GetProperty("hasMarketValuation").GetBoolean().ShouldBeFalse();
        body.GetProperty("pageNumber").GetInt32().ShouldBe(1);
        body.GetProperty("pageSize").GetInt32().ShouldBe(10);
        body.GetProperty("totalCount").GetInt32().ShouldBe(1);
        body.GetProperty("totalPages").GetInt32().ShouldBe(1);
    }

    [Theory]
    [InlineData("types=Inmueble")]
    [InlineData("purchasedFrom=2026-09-30&purchasedTo=2026-01-01")]
    [InlineData("minInvested=5000&maxInvested=100")]
    [InlineData("pageSize=500")]
    public async Task An_invalid_filter_is_rejected_with_400_without_querying_the_database(string query)
    {
        using var factory = new InvestmentsWebApplicationFactory();
        var client = AuthenticatedClient(factory);

        var response = await client.GetAsync(new Uri($"/api/investments?{query}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Investments.RequestedFilters.ShouldBeNull();
    }

    [Fact]
    public async Task Without_a_session_the_list_is_not_served()
    {
        using var factory = new InvestmentsWebApplicationFactory();

        var response = await factory.CreateClient().GetAsync(new Uri("/api/investments", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        factory.Investments.RequestedFilters.ShouldBeNull();
    }

    private static HttpClient AuthenticatedClient(InvestmentsWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(EmployeeId, Guid.CreateVersion7(), Rol.Empleado, "Ana Gomez");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class CapturingInvestmentReadRepository : IInvestmentReadRepository
    {
        public List<Investment> Investments { get; } = new();

        public Guid? RequestedEmployeeId { get; private set; }

        public InvestmentFilters? RequestedFilters { get; private set; }

        public Task<PagedResult<Investment>> GetPageAsync(Guid employeeId, InvestmentFilters filters, CancellationToken cancellationToken = default)
        {
            RequestedEmployeeId = employeeId;
            RequestedFilters = filters;

            var owned = Investments.Where(investment => investment.EmployeeId == employeeId).ToList();

            return Task.FromResult(new PagedResult<Investment>(owned, filters.PageNumber, filters.PageSize, owned.Count));
        }
    }

    private sealed class InvestmentsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public CapturingInvestmentReadRepository Investments { get; } = new();

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
                    ["TokenEncryption:Key"] = "dW5pdC10ZXN0LXRva2VuLWVuY3J5cHRpb24ta2V5ISE=",
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key-1234",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IInvestmentReadRepository>();
                services.AddSingleton<IInvestmentReadRepository>(Investments);
            });
        }
    }
}
