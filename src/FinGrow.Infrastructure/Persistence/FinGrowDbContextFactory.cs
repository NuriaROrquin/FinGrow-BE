namespace FinGrow.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public sealed class FinGrowDbContextFactory : IDesignTimeDbContextFactory<FinGrowDbContext>
{
    public FinGrowDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? "Host=localhost;Port=5432;Database=fingrow;Username=fingrow;Password=fingrow";

        var options = new DbContextOptionsBuilder<FinGrowDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(FinGrowDbContext).Assembly.FullName))
            .Options;

        return new FinGrowDbContext(options);
    }
}
