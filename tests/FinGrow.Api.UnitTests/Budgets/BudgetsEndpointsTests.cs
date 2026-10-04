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
    private const string SeptemberLimitsPath = "/api/budgets/2026/9/limits";

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

    [Fact]
public async Task Editing_a_limit_saves_it_and_returns_the_recalculated_budget()
{
    using var factory = new BudgetsWebApplicationFactory();
    var client = CreateAuthenticatedClient(factory, Rol.Empleado);
    await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());
    factory.Spent[ExpenseCategory.Alimentos] = 90000m;

    var response = await client.PutAsJsonAsync(
        new Uri(SeptemberLimitsPath, UriKind.Relative),
        new { category = "Alimentos", amount = 100000m });

    response.StatusCode.ShouldBe(HttpStatusCode.OK);
    factory.Budgets.Single().LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(100000m);
    var body = await response.Content.ReadAsStringAsync();
    body.ShouldContain("\"spent\":90000");
    body.ShouldContain("\"health\":\"Warning\"");
}

[Fact]
public async Task A_negative_limit_responds_400_with_a_clear_message()
{
    using var factory = new BudgetsWebApplicationFactory();
    var client = CreateAuthenticatedClient(factory, Rol.Empleado);
    await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

    var response = await client.PutAsJsonAsync(
        new Uri(SeptemberLimitsPath, UriKind.Relative),
        new { category = "Alimentos", amount = -500m });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await response.Content.ReadAsStringAsync()).ShouldContain("tiene que ser un numero mayor a cero");
    factory.Budgets.Single().LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(150000m);
}

[Fact]
public async Task A_non_numeric_limit_responds_400_with_a_clear_message()
{
    using var factory = new BudgetsWebApplicationFactory();
    var client = CreateAuthenticatedClient(factory, Rol.Empleado);
    await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

    var response = await client.PutAsJsonAsync(
        new Uri(SeptemberLimitsPath, UriKind.Relative),
        new { category = "Alimentos", amount = "mucho" });

    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    (await response.Content.ReadAsStringAsync()).ShouldContain("El campo 'amount' tiene que ser un numero.");
    factory.Budgets.Single().LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(150000m);
}

[Fact]
public async Task Editing_a_limit_of_a_month_without_a_budget_responds_404()
{
    using var factory = new BudgetsWebApplicationFactory();
    var client = CreateAuthenticatedClient(factory, Rol.Empleado);

    var response = await client.PutAsJsonAsync(
        new Uri(SeptemberLimitsPath, UriKind.Relative),
        new { category = "Alimentos", amount = 100000m });

    response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
}

        [Fact]
    public async Task Removing_a_category_takes_it_out_of_the_budget()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);
        await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        var response = await client.DeleteAsync(new Uri(SeptemberLimitsPath + "/Transporte", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = factory.Budgets.Single();
        stored.LimitFor(ExpenseCategory.Transporte).ShouldBeNull();
        stored.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(150000m);
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("Transporte");
    }

    [Fact]
    public async Task Removing_the_last_category_responds_409()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);
        await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());
        await client.DeleteAsync(new Uri(SeptemberLimitsPath + "/Transporte", UriKind.Relative));

        var response = await client.DeleteAsync(new Uri(SeptemberLimitsPath + "/Alimentos", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        factory.Budgets.Single().Limits.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Removing_a_category_that_is_not_in_the_budget_responds_404()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);
        await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        var response = await client.DeleteAsync(new Uri(SeptemberLimitsPath + "/Salud", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        factory.Budgets.Single().Limits.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Deleting_a_budget_removes_it_and_the_month_can_be_created_again()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);
        await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        var response = await client.DeleteAsync(new Uri(BudgetsPath + "/2026/9", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        factory.Budgets.ShouldBeEmpty();

        var recreated = await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());
        recreated.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deleting_the_budget_of_a_month_without_one_responds_404()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);

        var response = await client.DeleteAsync(new Uri(BudgetsPath + "/2026/9", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Changing_the_currency_keeps_the_amounts_and_returns_the_budget_in_the_new_one()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);
        await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        var response = await client.PutAsJsonAsync(
            new Uri(BudgetsPath + "/2026/9/currency", UriKind.Relative),
            new { currency = "USD" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = factory.Budgets.Single();
        stored.Currency.ShouldBe(Currency.USD);
        stored.LimitFor(ExpenseCategory.Alimentos)!.Amount.ShouldBe(150000m);
        (await response.Content.ReadAsStringAsync()).ShouldContain("\"currency\":\"USD\"");
    }

    [Fact]
    public async Task An_unknown_currency_responds_400()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);
        await client.PostAsJsonAsync(new Uri(BudgetsPath, UriKind.Relative), SeptemberBudget());

        var response = await client.PutAsJsonAsync(
            new Uri(BudgetsPath + "/2026/9/currency", UriKind.Relative),
            new { currency = "XYZ" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        factory.Budgets.Single().Currency.ShouldBe(Currency.ARS);
    }

    [Fact]
    public async Task Changing_the_currency_of_a_month_without_a_budget_responds_404()
    {
        using var factory = new BudgetsWebApplicationFactory();
        var client = CreateAuthenticatedClient(factory, Rol.Empleado);

        var response = await client.PutAsJsonAsync(
            new Uri(BudgetsPath + "/2026/9/currency", UriKind.Relative),
            new { currency = "USD" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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

        public Dictionary<ExpenseCategory, decimal> Spent { get; } = new();

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
                    ["Jobs:ApiKey"] = "unit-test-jobs-api-key",
                }));

            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IBudgetRepository>(_ => new InMemoryBudgetRepository(Budgets));
                services.AddScoped<IBudgetSpendingReadRepository>(_ => new InMemoryBudgetSpendingReadRepository(Spent));
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    private sealed class InMemoryBudgetRepository(List<Budget> budgets) : IBudgetRepository
    {
        public void Add(Budget budget) => budgets.Add(budget);

        public void Remove(Budget budget) => budgets.Remove(budget);

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

    private sealed class InMemoryBudgetSpendingReadRepository(Dictionary<ExpenseCategory, decimal> spent)
        : IBudgetSpendingReadRepository
    {
        public Task<IReadOnlyDictionary<ExpenseCategory, decimal>> GetSpentByCategoryAsync(
            Budget budget,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ExpenseCategory, decimal>>(spent);
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
