namespace FinGrow.Infrastructure.Persistence;

using System.Reflection;
using FinGrow.Application.Interfaces;
using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class FinGrowDbContext : DbContext, IUnitOfWork
{
    public FinGrowDbContext(DbContextOptions<FinGrowDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Budget> Budgets => Set<Budget>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<Investment> Investments => Set<Investment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Va al final a proposito: renombra lo que quedo con nombre por convencion, despues de
        // que cada configuracion haya puesto los nombres que si le importan.
        modelBuilder.UseSnakeCaseNames();

        base.OnModelCreating(modelBuilder);
    }
}
