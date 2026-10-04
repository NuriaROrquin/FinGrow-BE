namespace FinGrow.Api.Extensions;

using FinGrow.Infrastructure.Persistence;

public static partial class SeedExtensions
{
    public const string SeedOnStartupKey = "Database:SeedOnStartup";

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(SeedOnStartupKey))
        {
            LogDisabled(app.Logger, SeedOnStartupKey);
            return;
        }

        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seed al arrancar deshabilitado ({Key}=false).")]
    private static partial void LogDisabled(ILogger logger, string key);
}
