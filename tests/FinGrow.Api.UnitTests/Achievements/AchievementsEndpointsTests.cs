namespace FinGrow.Api.UnitTests.Achievements;

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

public class AchievementsEndpointsTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);
    private static readonly string[] PendingCodes = { "FirstTransaction", "FirstGoal" };

    [Fact]
    public async Task The_catalog_comes_with_the_code_that_picks_the_medal_and_the_date_of_what_was_earned()
    {
        using var factory = new AchievementsWebApplicationFactory();
        factory.Achievements.Add(EmployeeAchievement.Backfill(EmployeeId, AchievementCode.GoalAchieved, Now.AddDays(-3), Now));
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.GetAsync(new Uri("/api/achievements", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().ShouldBe(AchievementCatalog.All.Count);
        var goal = body.EnumerateArray().Single(item => item.GetProperty("code").GetString() == "GoalAchieved");
        goal.GetProperty("name").GetString().ShouldBe("Meta cumplida");
        goal.GetProperty("isMajor").GetBoolean().ShouldBeTrue();
        goal.GetProperty("condition").GetString().ShouldBe("Tiene al menos una meta alcanzada.");
        goal.GetProperty("achievedAt").GetDateTimeOffset().ShouldBe(Now.AddDays(-3));
        body.EnumerateArray().Single(item => item.GetProperty("code").GetString() == "Saver")
            .GetProperty("achievedAt").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task The_front_celebrates_what_is_pending_and_then_marks_it()
    {
        using var factory = new AchievementsWebApplicationFactory();
        factory.Achievements.Add(EmployeeAchievement.Backfill(EmployeeId, AchievementCode.FirstGoal, Now.AddDays(-3), Now));
        factory.Achievements.Add(EmployeeAchievement.Backfill(EmployeeId, AchievementCode.FirstTransaction, Now.AddDays(-9), Now));
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var pending = await client.GetFromJsonAsync<JsonElement>(new Uri("/api/achievements/uncelebrated", UriKind.Relative));
        var celebrate = await client.PostAsJsonAsync(
            new Uri("/api/achievements/celebrate", UriKind.Relative),
            new { codes = PendingCodes });

        pending.EnumerateArray().Select(item => item.GetProperty("code").GetString())
            .ShouldBe(PendingCodes);
        celebrate.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await celebrate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("celebrated").GetInt32().ShouldBe(2);
        factory.Achievements.Achievements.ShouldAllBe(achievement => achievement.IsCelebrated);
    }

    [Fact]
    public async Task Celebrating_without_codes_is_rejected()
    {
        using var factory = new AchievementsWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empleado);

        var response = await client.PostAsJsonAsync(
            new Uri("/api/achievements/celebrate", UriKind.Relative),
            new { codes = Array.Empty<string>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_company_never_sees_achievements()
    {
        using var factory = new AchievementsWebApplicationFactory();
        var client = AuthenticatedClient(factory, Rol.Empresa);

        var response = await client.GetAsync(new Uri("/api/achievements", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static HttpClient AuthenticatedClient(AchievementsWebApplicationFactory factory, string role)
    {
        using var scope = factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>()
            .GenerateToken(EmployeeId, Guid.CreateVersion7(), role, "Ana Gomez");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return client;
    }

    private sealed class InMemoryAchievementRepository : IAchievementRepository
    {
        public List<EmployeeAchievement> Achievements { get; } = new();

        public void Add(EmployeeAchievement achievement) => Achievements.Add(achievement);

        public Task<IReadOnlyList<EmployeeAchievement>> ListByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmployeeAchievement>>(Achievements.Where(achievement => achievement.EmployeeId == employeeId).ToList());

        public Task<IReadOnlyList<EmployeeAchievement>> ListUncelebratedAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EmployeeAchievement>>(Achievements
                .Where(achievement => achievement.EmployeeId == employeeId && !achievement.IsCelebrated)
                .ToList());
    }

    private sealed class AchievementsWebApplicationFactory : WebApplicationFactory<Program>
    {
        public InMemoryAchievementRepository Achievements { get; } = new();

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
                services.RemoveAll<IAchievementRepository>();
                services.AddSingleton<IAchievementRepository>(Achievements);
                services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
            });
        }
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
