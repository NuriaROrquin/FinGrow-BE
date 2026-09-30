namespace FinGrow.Api.UnitTests.Budgets;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinGrow.Application.Common;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class BudgetsEndpointsTests
{
    private const string BudgetsPath = "/api/budgets";
    private const string DuplicatePath = "/api/budgets/duplicate-previous";

    [Fact]
    public async Task Creating_a_budget_persists_it_for_the_authenticated_employee()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);

        var response = await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = factory.Budgets.ShouldHaveSingleItem();
        stored.EmployeeId.ShouldBe(factory.EmployeeId);
        stored.PeriodStart.ShouldBe(new DateOnly(2026, 9, 1));
        stored.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(150000m);
    }

    [Fact]
    public async Task Creating_a_second_budget_for_the_same_month_responds_409()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);
        await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        var response = await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        factory.Budgets.Count.ShouldBe(1);
    }

    [Fact]
    public async Task An_invalid_budget_responds_400()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);

        var response = await client.PostAsJsonAsync(
            new Uri(BudgetsPath, UriKind.Relative),
            new { year = 2026, month = 13, currency = "ARS", limits = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Budgets.ShouldBeEmpty();
    }

    [Fact]
    public async Task Duplicating_copies_the_previous_month_into_the_requested_one()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);
        await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        var response = await client.PostAsJsonAsync(
            new Uri(DuplicatePath, UriKind.Relative),
            new { year = 2026, month = 10 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var october = factory.Budgets.Single(budget => budget.PeriodStart == new DateOnly(2026, 10, 1));
        october.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(150000m);
        october.LimitFor(ExpenseCategory.Transporte)!.Amount.ShouldBe(40000m);
    }

    [Fact]
    public async Task Duplicating_without_a_previous_month_budget_responds_404()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);

        var response = await client.PostAsJsonAsync(
            new Uri(DuplicatePath, UriKind.Relative),
            new { year = 2026, month = 10 });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_budget_of_a_month_can_be_read_after_creating_it()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);
        await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        var response = await client.GetAsync(new Uri(BudgetsPath + "/2026/9", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"periodStart\":\"2026-09-01\"");
        body.ShouldContain("\"category\":\"Alimentos\"");
    }

    [Fact]
    public async Task Reading_a_month_without_a_budget_responds_404()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri(BudgetsPath + "/2026/9", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_company_token_is_rejected_with_403()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empresa);

        var response = await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        factory.Budgets.ShouldBeEmpty();
    }

    private static object SeptemberBudget() => new
    {
        year = 2026,
        month = 9,
        currency = "ARS",
        limits = new[]
        {
            new { category = "Alimentos", amount = 150000m },
            new { category = "Transporte", amount = 40000m },
        },
    };

    private static HttpClient CreateAuthenticatedClient(BudgetsWebApplicationFactory factory, string role)
    {
        var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(factory.EmployeeId, Guid.CreateVersion7(), role, "Juan Perez");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class BudgetsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Guid EmployeeId { get; } = Guid.CreateVersion7();

        public List<Budget> Budgets { get; } = new();

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
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IBudgetRepository>(_ => new InMemoryBudgetRepository(Budgets));
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    private sealed class InMemoryBudgetRepository(List<Budget> budgets) : IBudgetRepository
    {
        public void Add(Budget budget) => budgets.Add(budget);

        public Task<bool> ExistsForPeriodAsync(
            Guid employeeId,
            BudgetPeriod period,
            DateOnly periodStart,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(budgets.Exists(budget =>
                budget.EmployeeId == employeeId && budget.Period == period && budget.PeriodStart == periodStart));

        public Task<Budget?> FindForPeriodAsync(
            Guid employeeId,
            BudgetPeriod period,
            DateOnly periodStart,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(budgets.FirstOrDefault(budget =>
                budget.EmployeeId == employeeId && budget.Period == period && budget.PeriodStart == periodStart));
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
