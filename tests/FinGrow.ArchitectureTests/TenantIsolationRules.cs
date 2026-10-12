namespace FinGrow.ArchitectureTests;

using FinGrow.Application.Interfaces;
using FinGrow.Infrastructure.Persistence;
using FinGrow.Infrastructure.Persistence.Protection;
using Microsoft.EntityFrameworkCore;

public class TenantIsolationRules
{
    private static readonly Guid CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Every_entity_applies_the_tenant_filter_or_is_declared_global()
    {
        using var context = CreateContext(new FakeCurrentUser(CompanyId));

        var unprotected = context.Model.GetEntityTypes()
            .Where(entity => !entity.IsOwned())
            .Where(entity => !HasTenantFilter(entity))
            .Where(entity => !FinGrowDbContext.GlobalEntityTypes.Contains(entity.ClrType))
            .Select(entity => entity.ClrType.Name)
            .ToList();

        unprotected.ShouldBeEmpty(
            "Estas entidades no aplican el filtro de empresa. Agregalo en FinGrowDbContext.Tenancy.cs "
            + "o, si de verdad no pertenecen a ninguna empresa, sumalas a GlobalEntityTypes.");
    }

    [Fact]
    public void Global_entities_must_exist_in_the_model_and_must_not_be_filtered()
    {
        using var context = CreateContext(new FakeCurrentUser(CompanyId));

        foreach (var type in FinGrowDbContext.GlobalEntityTypes)
        {
            var entity = context.Model.FindEntityType(type);

            entity.ShouldNotBeNull($"{type.Name} figura como global pero no existe en el modelo.");
            HasTenantFilter(entity).ShouldBeFalse($"{type.Name} figura como global pero tiene filtro de empresa.");
        }
    }

    [Fact]
    public void Queries_are_restricted_to_the_company_of_the_current_user()
    {
        using var context = CreateContext(new FakeCurrentUser(CompanyId));

        var queries = new[]
        {
            context.Employees.ToQueryString(),
            context.Departments.ToQueryString(),
            context.Transactions.ToQueryString(),
            context.Goals.ToQueryString(),
            context.Notifications.ToQueryString(),
        };

        foreach (var sql in queries)
        {
            sql.ShouldContain(CompanyId.ToString());
        }
    }

    [Fact]
    public void Queries_of_a_user_without_company_are_not_unscoped()
    {
        using var context = CreateContext(new FakeCurrentUser(null));

        var sql = context.Employees.ToQueryString();

        sql.ShouldContain("IsUnscoped");
        sql.ShouldContain("='False'");
    }

    private static bool HasTenantFilter(Microsoft.EntityFrameworkCore.Metadata.IEntityType entity) =>
        entity.GetDeclaredQueryFilters().Any(filter => filter.Key == FinGrowDbContext.TenantFilter);

    private static FinGrowDbContext CreateContext(ICurrentUser currentUser)
    {
        var options = new DbContextOptionsBuilder<FinGrowDbContext>()
            .UseNpgsql("Host=localhost;Database=fingrow_tests")
            .Options;

        return new FinGrowDbContext(
            options,
            new AesGcmSecretProtector(new byte[TokenEncryptionOptions.KeyBytes]),
            currentUser);
    }

    private sealed class FakeCurrentUser(Guid? companyId) : ICurrentUser
    {
        public Guid? UserId => Guid.NewGuid();

        public Guid? CompanyId => companyId;

        public string? FullName => "Test";

        public string? Role => "Empleado";

        public DateTimeOffset? ExpiresAt => null;

        public bool IsAuthenticated => true;
    }
}
