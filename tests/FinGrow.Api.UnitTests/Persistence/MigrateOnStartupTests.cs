namespace FinGrow.Api.UnitTests.Persistence;

using FinGrow.Api.Extensions;
using FinGrow.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Npgsql;

public class MigrateOnStartupTests
{
    [Fact]
    public async Task Does_nothing_when_migrations_on_startup_are_disabled()
    {
        await using var app = BuildApp(migrateOnStartup: false);

        await Should.NotThrowAsync(() => app.MigrateDatabaseAsync());
    }

    [Fact]
    public async Task Fails_when_pending_migrations_cannot_be_applied()
    {
        await using var app = BuildApp(migrateOnStartup: true);

        await Should.ThrowAsync<NpgsqlException>(() => app.MigrateDatabaseAsync());
    }

    private static WebApplication BuildApp(bool migrateOnStartup)
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=localhost;Port=1;Database=fingrow;Username=fingrow;Password=fingrow;Timeout=1",
            [MigrationExtensions.MigrateOnStartupKey] = migrateOnStartup ? "true" : "false",
            ["AiService:BaseUrl"] = "http://localhost:8000",
            ["AiService:ApiKey"] = "unit-test-ai-api-key",
            ["Jwt:SecretKey"] = "unit-test-secret-key-at-least-32-characters-long",
            ["Jwt:Issuer"] = "FinGrow",
            ["Jwt:Audience"] = "FinGrow",
        });
        builder.Services.AddInfrastructure(builder.Configuration);

        return builder.Build();
    }
}
