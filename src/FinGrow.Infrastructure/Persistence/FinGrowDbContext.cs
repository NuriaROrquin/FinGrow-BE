namespace FinGrow.Infrastructure.Persistence;

using System.Reflection;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using FinGrow.Infrastructure.Persistence.Protection;
using Microsoft.EntityFrameworkCore;

public sealed class FinGrowDbContext : DbContext, IUnitOfWork
{
    private readonly ISecretProtector _protector;

    public FinGrowDbContext(DbContextOptions<FinGrowDbContext> options, ISecretProtector protector) : base(options) =>
        _protector = protector;

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<BudgetCategoryLimit> BudgetCategoryLimits => Set<BudgetCategoryLimit>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<GoalContribution> GoalContributions => Set<GoalContribution>();

    public DbSet<Investment> Investments => Set<Investment>();

    public DbSet<InvestmentValuation> InvestmentValuations => Set<InvestmentValuation>();

    public DbSet<EmployeeIntegration> EmployeeIntegrations => Set<EmployeeIntegration>();

    public DbSet<IntegrationLinkCode> IntegrationLinkCodes => Set<IntegrationLinkCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        var encrypted = new EncryptedStringConverter(_protector);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entity => entity.GetProperties())
                     .Where(property => property.FindAnnotation(EncryptedStringConverter.Annotation)?.Value is true))
        {
            property.SetValueConverter(encrypted);
        }

        // Va al final a proposito: renombra lo que quedo con nombre por convencion, despues de
        // que cada configuracion haya puesto los nombres que si le importan.
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
