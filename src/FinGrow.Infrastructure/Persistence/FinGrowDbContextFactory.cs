namespace FinGrow.Infrastructure.Persistence;

using FinGrow.Application.Interfaces;
using FinGrow.Infrastructure.Persistence.Protection;
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

        return new FinGrowDbContext(
            options,
            new AesGcmSecretProtector(new byte[TokenEncryptionOptions.KeyBytes]),
            new DesignTimeCurrentUser());
    }

    // Las herramientas de EF no tienen sesion: solo necesitan armar el modelo.
    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public Guid? UserId => null;

        public Guid? CompanyId => null;

        public string? FullName => null;

        public string? Role => null;

        public DateTimeOffset? ExpiresAt => null;

        public bool IsAuthenticated => false;
    }
}
