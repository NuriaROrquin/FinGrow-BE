namespace FinGrow.Api.UnitTests.Ai;

using FinGrow.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class AiServiceHealthCheckTests
{
    [Fact]
    public async Task Ai_check_is_healthy_when_the_ai_service_responds()
    {
        using var factory = new AiHealthWebApplicationFactory(aiIsUp: true);

        var report = await RunAiCheckAsync(factory);

        report.Entries["ai"].Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Ai_check_is_degraded_not_unhealthy_when_the_ai_service_is_down()
    {
        using var factory = new AiHealthWebApplicationFactory(aiIsUp: false);

        var report = await RunAiCheckAsync(factory);

        report.Entries["ai"].Status.ShouldBe(HealthStatus.Degraded);
    }

    private static Task<HealthReport> RunAiCheckAsync(WebApplicationFactory<Program> factory) =>
        factory.Services.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Name == "ai");

    private sealed class StubAiService : IAiService
    {
        private readonly bool _isUp;

        public StubAiService(bool isUp) => _isUp = isUp;

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default) => Task.FromResult(_isUp);

        public Task<IReadOnlyList<CategorizedExpense>> CategorizeExpensesAsync(
            IReadOnlyList<ExpenseToCategorize> expenses,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class AiHealthWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly bool _aiIsUp;

        public AiHealthWebApplicationFactory(bool aiIsUp) => _aiIsUp = aiIsUp;

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
                services.AddTransient<IAiService>(_ => new StubAiService(_aiIsUp)));
        }
    }
}
