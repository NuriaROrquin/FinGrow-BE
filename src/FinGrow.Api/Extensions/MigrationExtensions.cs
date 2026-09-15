namespace FinGrow.Api.Extensions;

using FinGrow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public static partial class MigrationExtensions
{
    public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>(MigrateOnStartupKey))
        {
            LogDisabled(app.Logger, MigrateOnStartupKey);
            return;
        }

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinGrowDbContext>();

        var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            LogUpToDate(app.Logger);
            return;
        }

        LogApplying(app.Logger, pending.Count, pending);
        await dbContext.Database.MigrateAsync();
        LogApplied(app.Logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Migraciones al arrancar deshabilitadas ({Key}=false).")]
    private static partial void LogDisabled(ILogger logger, string key);

    [LoggerMessage(Level = LogLevel.Information, Message = "La base de datos está al día; no hay migraciones pendientes.")]
    private static partial void LogUpToDate(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Aplicando {Count} migración(es) pendiente(s): {Migrations}")]
    private static partial void LogApplying(ILogger logger, int count, IReadOnlyList<string> migrations);

    [LoggerMessage(Level = LogLevel.Information, Message = "Migraciones aplicadas.")]
    private static partial void LogApplied(ILogger logger);
}
