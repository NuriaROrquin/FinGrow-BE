namespace FinGrow.Api.UnitTests.Reports;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
using Microsoft.Extensions.DependencyInjection.Extensions;

public class ReportsEndpointsTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();

    [Fact]
    public async Task The_monthly_summary_comes_month_by_month_from_the_first_month_with_movements()
    {
        using var factory = new ReportsWebApplicationFactory();
        factory.Summary.Totals.Add(new MonthlyMovementTotal(2026, 9, TransactionType.Income, null, 1000m));
        factory.Summary.Totals.Add(new MonthlyMovementTotal(2026, 9, TransactionType.Expense, ExpenseCategory.Alimentos, 400m));
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/reports/monthly-summary?currency=ARS&from=2026-08-01&to=2026-09-30", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("months").GetArrayLength().ShouldBe(1);
        var september = body.GetProperty("months")[0];
        september.GetProperty("month").GetInt32().ShouldBe(9);
        september.GetProperty("income").GetDecimal().ShouldBe(1000m);
        september.GetProperty("savings").GetDecimal().ShouldBe(600m);
        september.GetProperty("expensesByCategory")[0].GetProperty("category").GetString().ShouldBe("Alimentos");
        body.GetProperty("averageMonthlySavings").GetDecimal().ShouldBe(600m);
        var highlights = body.GetProperty("highlights");
        highlights.GetProperty("highestIncomeMonth").GetProperty("month").GetInt32().ShouldBe(9);
        highlights.GetProperty("bestSavingsMonth").GetProperty("amount").GetDecimal().ShouldBe(600m);
    }

    [Fact]
    public async Task Without_enough_history_the_insights_say_what_is_missing()
    {
        using var factory = new ReportsWebApplicationFactory();
        factory.Summary.Activity.Add(new MonthlyActivity(2026, 9, 12, true));
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/reports/insights?currency=ARS&from=2026-10-01&to=2026-10-15", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var eligibility = body.GetProperty("eligibility");
        eligibility.GetProperty("isEligible").GetBoolean().ShouldBeFalse();
        eligibility.GetProperty("activeMonths").GetInt32().ShouldBe(1);
        eligibility.GetProperty("requiredActiveMonths").GetInt32().ShouldBe(2);
        eligibility.GetProperty("requiredTransactions").GetInt32().ShouldBe(10);
        body.GetProperty("highlights").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("achievements").GetArrayLength().ShouldBe(0);
        body.GetProperty("recommendations").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task The_expense_breakdown_separates_savings_from_the_ranking()
    {
        using var factory = new ReportsWebApplicationFactory();
        factory.Summary.Totals.Add(new MonthlyMovementTotal(2026, 9, TransactionType.Expense, ExpenseCategory.Alimentos, 300m));
        factory.Summary.Totals.Add(new MonthlyMovementTotal(2026, 9, TransactionType.Expense, ExpenseCategory.Vivienda, 100m));
        factory.Summary.Totals.Add(new MonthlyMovementTotal(2026, 9, TransactionType.Expense, ExpenseCategory.AhorroInversion, 600m));
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/reports/expense-breakdown?currency=ARS&from=2026-09-01&to=2026-09-30", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("previousFrom").GetString().ShouldBe("2026-08-01");
        body.GetProperty("previousTo").GetString().ShouldBe("2026-08-31");
        body.GetProperty("hasPreviousPeriod").GetBoolean().ShouldBeFalse();
        var categories = body.GetProperty("categories");
        categories.GetArrayLength().ShouldBe(2);
        categories[0].GetProperty("category").GetString().ShouldBe("Alimentos");
        categories[0].GetProperty("percentage").GetDecimal().ShouldBe(75.0m);
        categories[0].GetProperty("comparison").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("consumption").GetProperty("amount").GetDecimal().ShouldBe(400m);
        body.GetProperty("savings").GetProperty("amount").GetDecimal().ShouldBe(600m);
        body.GetProperty("total").GetProperty("amount").GetDecimal().ShouldBe(1000m);
    }

    [Theory]
    [InlineData("/api/reports/insights?currency=ARS&to=2026-10-15")]
    [InlineData("/api/reports/insights?currency=ARS&from=2026-10-15&to=2026-10-01")]
    [InlineData("/api/reports/monthly-summary?currency=ARS&from=2020-01-01&to=2026-10-15")]
    [InlineData("/api/reports/expense-breakdown?currency=ARS&from=2026-09-01")]
    [InlineData("/api/reports/expense-breakdown?currency=ARS&from=2026-09-30&to=2026-09-01")]
    public async Task A_missing_or_invalid_period_is_rejected(string url)
    {
        using var factory = new ReportsWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/api/reports/insights?from=2026-10-01&to=2026-10-15")]
    [InlineData("/api/reports/monthly-summary?from=2026-10-01&to=2026-10-15")]
    [InlineData("/api/reports/expense-breakdown?from=2026-10-01&to=2026-10-15")]
    public async Task A_company_cannot_read_an_employee_report(string url)
    {
        using var factory = new ReportsWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empresa);

        var response = await client.GetAsync(new Uri(url, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static HttpClient AuthenticatedClient(ReportsWebApplicationFactory factory, string role)
    {
        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(EmployeeId, Guid.CreateVersion7(), role, "Ana Gomez");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class InMemoryMonthlySummaryReadRepository : IMonthlySummaryReadRepository
    {
        public List<MonthlyMovementTotal> Totals { get; } = new();

        public List<MonthlyActivity> Activity { get; } = new();

        public Task<IReadOnlyList<MonthlyMovementTotal>> ListMonthlyTotalsAsync(
            Guid employeeId,
            Currency currency,
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MonthlyMovementTotal>>(Totals.ToList());

        public Task<DateOnly?> FindFirstMovementDateAsync(Guid employeeId, Currency currency, CancellationToken cancellationToken = default) =>
            Task.FromResult(Totals
                .Select(total => (DateOnly?)new DateOnly(total.Year, total.Month, 1))
                .Min());

        public Task<IReadOnlyList<MonthlyActivity>> ListMonthlyActivityAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MonthlyActivity>>(Activity.ToList());
    }

    // Sin empleado habilitado, insights igual lista los logros del período: no tiene que ir a la base.
    private sealed class EmptyAchievementRepository : IAchievementRepository
    {
        public void Add(EmployeeAchievement achievement)
        {
        }

        public Task<IReadOnlyList<EmployeeAchievement>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmployeeAchievement>>(Array.Empty<EmployeeAchievement>());

        public Task<IReadOnlyList<EmployeeAchievement>> ListUncelebratedAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmployeeAchievement>>(Array.Empty<EmployeeAchievement>());
    }

    private sealed class ReportsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public InMemoryMonthlySummaryReadRepository Summary { get; } = new();

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
                services.RemoveAll<IMonthlySummaryReadRepository>();
                services.AddSingleton<IMonthlySummaryReadRepository>(Summary);
                services.RemoveAll<IAchievementRepository>();
                services.AddSingleton<IAchievementRepository>(new EmptyAchievementRepository());
            });
        }
    }
}
